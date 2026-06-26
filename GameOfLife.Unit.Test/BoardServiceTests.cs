using GameOfLife.Api.Data;
using GameOfLife.Api.Data.Objects;
using GameOfLife.Api.Service;

namespace GameOfLife.Unit.Test;

[TestFixture]
public class BoardServiceTests
{
    private IBoardRepository _repository = null!;
    private IBoardStepper _stepper = null!;
    private BoardService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IBoardRepository>();
        _stepper = Substitute.For<IBoardStepper>();
        _service = new BoardService(_repository, _stepper);
    }

    #region CreateNewBoard
    // ---------------------------------------------------------------------
    // CreateNewBoard
    // ---------------------------------------------------------------------

    [Test]
    public void CreateNewBoard_StoresLiveCellsAsBothInitialAndCurrentState()
    {
        Cell[] cells = [new Cell(0, 0), new Cell(1, 0), new Cell(2, 0)];
        var captured = CaptureCreatedState(returnId: 7);

        var id = _service.CreateNewBoard(cells);

        var state = captured();
        Assert.That(id, Is.EqualTo(7));
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.InitialState, Is.EquivalentTo(cells));
        Assert.That(state.CurrentState, Is.EquivalentTo(cells));
        Assert.That(state.IterationCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateNewBoard_InitialAndCurrentAreIndependentCopies()
    {
        Cell[] cells = [new Cell(0, 0)];
        var captured = CaptureCreatedState(returnId: 1);

        var id = _service.CreateNewBoard(cells);

        var state = captured();
        Assert.That(id, Is.EqualTo(1));
        // Mutating the current state must not bleed into the initial state.
        state!.CurrentState.Add(new Cell(99, 99));
        Assert.That(state.InitialState, Does.Not.Contain(new Cell(99, 99)));
    }

    [Test]
    public void CreateNewBoard_DeduplicatesRepeatedCells()
    {
        Cell[] cells = [new Cell(5, 5), new Cell(5, 5), new Cell(5, 5)];
        var captured = CaptureCreatedState(returnId: 1);

        var id = _service.CreateNewBoard(cells);

        var state = captured();
        Assert.That(id, Is.EqualTo(1));
        Assert.That(state!.InitialState.Count, Is.EqualTo(1));
        Assert.That(state.CurrentState.Count, Is.EqualTo(1));
    }

    [Test]
    public void CreateNewBoard_EmptyInput_StoresEmptyState()
    {
        var captured = CaptureCreatedState(returnId: 1);

        var id = _service.CreateNewBoard(Array.Empty<Cell>());

        var state = captured();
        Assert.That(id, Is.EqualTo(1));
        Assert.That(state!.InitialState, Is.Empty);
        Assert.That(state.CurrentState, Is.Empty);
        Assert.That(state.IterationCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateNewBoard_ReturnsIdFromRepository()
    {
        _repository.CreateNewBoard(Arg.Any<BoardState>()).Returns(42);

        var id = _service.CreateNewBoard([new Cell(0, 0)]);

        Assert.That(id, Is.EqualTo(42));
        _repository.Received(1).CreateNewBoard(Arg.Any<BoardState>());
    }

    [Test]
    public void CreateNewBoard_NullLiveCells_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.CreateNewBoard(null!));
    }

    #endregion

    #region ConvertGridToLiveCells

    // ---------------------------------------------------------------------
    // ConvertGridToLiveCells
    // ---------------------------------------------------------------------

    [Test]
    public void ConvertGridToLiveCells_MapsRowColumnToXY()
    {
        // A single live cell at row 1, column 2 -> Cell(x: 2, y: 1).
        bool[][] grid =
        [
            [false, false, false],
            [false, false, true],
        ];

        var cells = _service.ConvertGridToLiveCells(grid);

        Assert.That(cells, Is.EquivalentTo(new[] { new Cell(2, 1) }));
    }

    [Test]
    public void ConvertGridToLiveCells_Block_AllAlive()
    {
        bool[][] grid =
        [
            [true, true],
            [true, true],
        ];

        var cells = _service.ConvertGridToLiveCells(grid);

        Assert.That(cells, Is.EquivalentTo(new[]
        {
            new Cell(0, 0), new Cell(1, 0),
            new Cell(0, 1), new Cell(1, 1),
        }));
    }

    [Test]
    public void ConvertGridToLiveCells_AllDead_ReturnsEmpty()
    {
        bool[][] grid =
        [
            [false, false],
            [false, false],
        ];

        Assert.That(_service.ConvertGridToLiveCells(grid), Is.Empty);
    }

    [Test]
    public void ConvertGridToLiveCells_EmptyGrid_ReturnsEmpty()
    {
        Assert.That(_service.ConvertGridToLiveCells(Array.Empty<bool[]>()), Is.Empty);
    }

    [Test]
    public void ConvertGridToLiveCells_JaggedRows_UsesEachRowsOwnLength()
    {
        // Rows of differing length. A naive grid[0].Length bound would drop the
        // cells in the longer second row (and throw if the first row were the long one).
        bool[][] grid =
        [
            [true],                  // row 0: width 1
            [false, true, true],     // row 1: width 3
        ];

        var cells = _service.ConvertGridToLiveCells(grid);

        Assert.That(cells, Is.EquivalentTo(new[]
        {
            new Cell(0, 0),
            new Cell(1, 1), new Cell(2, 1),
        }));
    }

    [Test]
    public void ConvertGridToLiveCells_NullGrid_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.ConvertGridToLiveCells(null!));
    }

    [Test]
    public void ConvertGridToLiveCells_NullRow_ThrowsArgumentException()
    {
        bool[][] grid = [[true], null!];
        Assert.Throws<ArgumentException>(() => _service.ConvertGridToLiveCells(grid));
    }

    #endregion

    #region Still Lifes

    [Test]
    public void FinalizeBoard_StillLifeBlock_ConcludesAfterOneIteration()
    {
        int id = 1234;  // arbitrary id
        Cell[] cells = [new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(1, 1)];    // arbitrary cells, since we mock the Step method

        _repository.GetExistingBoard(id).Returns(CellsToBoardState(cells));

        // A still life is unchanged by a step, so Step returns the same live cells it was given.
        // (Step is mocked, so the specific cells are arbitrary — this test only exercises
        // FinalizeBoard's control flow: it should conclude as soon as a step changes nothing.)
        _stepper.Step(Arg.Any<IReadOnlySet<Cell>>()).Returns(new HashSet<Cell>(cells));

        // A still life is unchanged, so the final state is the same cells after exactly one
        // algorithm run (the run that confirms nothing changed).
        var expected = CellsToBoardState(cells, iterationCount: 1);

        // setting max iterations to 10 so that we aren't testing two things at the same time (whether budget is big enough and the actual iteration count).
        // For this test, we only want to test that the actual iteration count is correct.  Testing the max budget will be handled in other tests.
        BoardState actual = _service.FinalizeBoard(id, 10);

        Assert.That(actual, Is.EqualTo(expected).Using<BoardState>(BoardStatesEqual));
        _repository.Received(1).UpdateExistingBoard(id, Arg.Is<BoardState>(s => BoardStatesEqual(s, expected)));
    }

    #endregion

    #region Non-concluding boards

    [Test]
    public void FinalizeBoard_NeverSettles_ThrowsWhenBudgetExhausted()
    {
        int id = 1234;          // arbitrary id
        int maxIterations = 5;  // small budget so the test is fast and the count is easy to verify

        // A period-2 oscillator (think blinker): the board flips between two distinct phases and
        // never reaches a fixed point. Step is mocked to toggle between two non-equal sets, so
        // every step changes something and FinalizeBoard can never conclude.
        Cell[] phaseA = [new Cell(0, 0)];
        Cell[] phaseB = [new Cell(9, 9)];
        _repository.GetExistingBoard(id).Returns(CellsToBoardState(phaseA));

        int callCount = 0;
        _stepper.Step(Arg.Any<IReadOnlySet<Cell>>())
            .Returns(_ => new HashSet<Cell>(callCount++ % 2 == 0 ? phaseB : phaseA));

        var ex = Assert.Throws<BoardDidNotConcludeException>(() => _service.FinalizeBoard(id, maxIterations));

        // The exception carries the context the Web layer needs to build its error response.
        Assert.That(ex!.BoardId, Is.EqualTo(id));
        Assert.That(ex.MaxIterationCount, Is.EqualTo(maxIterations));

        // The full budget is spent: Step is called exactly maxIterations times...
        _stepper.Received(maxIterations).Step(Arg.Any<IReadOnlySet<Cell>>());
        // ...and progress is persisted before giving up, so a later retry with a bigger budget can resume.
        _repository.Received(1).UpdateExistingBoard(id, Arg.Is<BoardState>(s => s.IterationCount == maxIterations));
    }

    #endregion

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Arranges the repository mock to capture the BoardState handed to CreateNewBoard
    /// and return the given id. Returns an accessor for the captured state.
    /// </summary>
    private Func<BoardState?> CaptureCreatedState(int returnId)
    {
        BoardState? captured = null;
        _repository.CreateNewBoard(Arg.Do<BoardState>(s => captured = s)).Returns(returnId);
        return () => captured;
    }

    private BoardState CellsToBoardState(Cell[] cells, int iterationCount = 0)
    {
        var result = new BoardState
        {
            InitialState = new HashSet<Cell>(cells),
            CurrentState = new HashSet<Cell>(cells),
            IterationCount = iterationCount
        };

        return result;
    }

    /// <summary>
    /// Value comparison for two board states: same iteration count and the same set of live cells
    /// in both their initial and current states. Lives here rather than on BoardState so the
    /// domain type doesn't have to carry a mutable GetHashCode just to support equality.
    /// </summary>
    private static bool BoardStatesEqual(BoardState a, BoardState b) =>
        a.IterationCount == b.IterationCount
        && a.InitialState.SetEquals(b.InitialState)
        && a.CurrentState.SetEquals(b.CurrentState);
}
