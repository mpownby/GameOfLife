using GameOfLife.Api.Data;
using GameOfLife.Api.Data.Objects;
using GameOfLife.Api.Service;

namespace GameOfLife.Unit.Test;

[TestFixture]
public class BoardServiceTests
{
    private IBoardRepository _repository = null!;
    private BoardService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IBoardRepository>();
        _service = new BoardService(_repository);
    }

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
}
