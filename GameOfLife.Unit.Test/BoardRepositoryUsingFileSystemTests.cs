using GameOfLife.Api.Data;
using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Unit.Test;

[TestFixture]
public class BoardRepositoryUsingFileSystemTests
{
    private IFileIOPassThrough _fileIO = null!;

    [SetUp]
    public void SetUp()
    {
        _fileIO = Substitute.For<IFileIOPassThrough>();
        _fileIO.Exists(Arg.Any<string>()).Returns(false);   // no persisted file unless a test says otherwise
    }

    private BoardRepositoryUsingFileSystem CreateRepository(string path = "boards.json") => new(_fileIO, path);

    private static BoardState StateWith(params Cell[] cells) => new()
    {
        InitialState = new HashSet<Cell>(cells),
        CurrentState = new HashSet<Cell>(cells),
        IterationCount = 0
    };

    // ---------------------------------------------------------------------
    // Construction
    // ---------------------------------------------------------------------

    [Test]
    public void Constructor_NullFileIO_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new BoardRepositoryUsingFileSystem(null!, "boards.json"));
    }

    [Test]
    public void Constructor_BlankPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new BoardRepositoryUsingFileSystem(_fileIO, "  "));
    }

    [Test]
    public void Constructor_EnsuresTargetDirectoryExists()
    {
        CreateRepository(Path.Combine("data", "boards.json"));

        _fileIO.Received().CreateDirectory(Arg.Any<string>());
    }

    [Test]
    public void Constructor_NoExistingFile_StartsEmpty()
    {
        var repo = CreateRepository();

        // first board created in an empty repo gets id 0
        Assert.That(repo.CreateNewBoard(StateWith(new Cell(0, 0))), Is.EqualTo(0));
        _fileIO.DidNotReceive().ReadAllText(Arg.Any<string>());
    }

    [Test]
    public void Constructor_LoadsPersistedBoards_RoundTripThroughJson()
    {
        // First repository persists a board; capture the JSON it writes.
        string? written = null;
        _fileIO.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
               .Do(ci => written = ci.ArgAt<string>(1));
        var first = CreateRepository();
        var id = first.CreateNewBoard(StateWith(new Cell(7, 8)));

        // A brand-new repository whose file layer hands back that JSON must rebuild the same board.
        var reloadFileIO = Substitute.For<IFileIOPassThrough>();
        reloadFileIO.Exists(Arg.Any<string>()).Returns(true);
        reloadFileIO.ReadAllText(Arg.Any<string>()).Returns(written);
        var second = new BoardRepositoryUsingFileSystem(reloadFileIO, "boards.json");

        Assert.That(second.GetExistingBoard(id).InitialState, Is.EquivalentTo(new[] { new Cell(7, 8) }));
    }

    // ---------------------------------------------------------------------
    // CreateNewBoard
    // ---------------------------------------------------------------------

    [Test]
    public void CreateNewBoard_AssignsSequentialIdsStartingAtZero()
    {
        var repo = CreateRepository();

        Assert.That(repo.CreateNewBoard(StateWith(new Cell(0, 0))), Is.EqualTo(0));
        Assert.That(repo.CreateNewBoard(StateWith(new Cell(1, 1))), Is.EqualTo(1));
        Assert.That(repo.CreateNewBoard(StateWith(new Cell(2, 2))), Is.EqualTo(2));
    }

    [Test]
    public void CreateNewBoard_PersistsAtomically_WritesTempThenMovesOverTarget()
    {
        var repo = CreateRepository();

        repo.CreateNewBoard(StateWith(new Cell(0, 0)));

        Received.InOrder(() =>
        {
            _fileIO.WriteAllText(Arg.Any<string>(), Arg.Any<string>());
            _fileIO.Move(Arg.Any<string>(), Arg.Any<string>(), true);
        });
    }

    [Test]
    public void CreateNewBoard_NullSource_ThrowsArgumentNullException()
    {
        var repo = CreateRepository();

        Assert.Throws<ArgumentNullException>(() => repo.CreateNewBoard(null!));
    }

    [Test]
    public void CreateNewBoard_StoresIndependentCopyOfSource()
    {
        var repo = CreateRepository();
        var src = StateWith(new Cell(0, 0));

        var id = repo.CreateNewBoard(src);
        src.CurrentState.Add(new Cell(9, 9));     // mutate the source after storing it

        Assert.That(repo.GetExistingBoard(id).CurrentState, Does.Not.Contain(new Cell(9, 9)));
    }

    // ---------------------------------------------------------------------
    // GetExistingBoard
    // ---------------------------------------------------------------------

    [Test]
    public void GetExistingBoard_ReturnsStoredBoard()
    {
        var repo = CreateRepository();
        var id = repo.CreateNewBoard(StateWith(new Cell(3, 4)));

        var board = repo.GetExistingBoard(id);

        Assert.That(board.InitialState, Is.EquivalentTo(new[] { new Cell(3, 4) }));
    }

    [Test]
    public void GetExistingBoard_ReturnsIndependentCopy()
    {
        var repo = CreateRepository();
        var id = repo.CreateNewBoard(StateWith(new Cell(0, 0)));

        repo.GetExistingBoard(id).CurrentState.Add(new Cell(9, 9));   // mutate the returned copy

        Assert.That(repo.GetExistingBoard(id).CurrentState, Does.Not.Contain(new Cell(9, 9)));
    }

    [Test]
    public void GetExistingBoard_InvalidId_ThrowsKeyNotFound()
    {
        var repo = CreateRepository();

        Assert.Throws<KeyNotFoundException>(() => repo.GetExistingBoard(0));
        Assert.Throws<KeyNotFoundException>(() => repo.GetExistingBoard(-1));
    }

    // ---------------------------------------------------------------------
    // UpdateExistingBoard
    // ---------------------------------------------------------------------

    [Test]
    public void UpdateExistingBoard_ReplacesAndPersists()
    {
        var repo = CreateRepository();
        var id = repo.CreateNewBoard(StateWith(new Cell(0, 0)));
        _fileIO.ClearReceivedCalls();

        var updated = StateWith(new Cell(5, 5));
        updated.IterationCount = 3;
        repo.UpdateExistingBoard(id, updated);

        var board = repo.GetExistingBoard(id);
        Assert.That(board.CurrentState, Is.EquivalentTo(new[] { new Cell(5, 5) }));
        Assert.That(board.IterationCount, Is.EqualTo(3));
        _fileIO.Received().WriteAllText(Arg.Any<string>(), Arg.Any<string>());
    }

    [Test]
    public void UpdateExistingBoard_InvalidId_ThrowsKeyNotFound()
    {
        var repo = CreateRepository();

        Assert.Throws<KeyNotFoundException>(
            () => repo.UpdateExistingBoard(0, StateWith(new Cell(0, 0))));
    }

    [Test]
    public void UpdateExistingBoard_NullState_ThrowsArgumentNullException()
    {
        var repo = CreateRepository();
        var id = repo.CreateNewBoard(StateWith(new Cell(0, 0)));

        Assert.Throws<ArgumentNullException>(() => repo.UpdateExistingBoard(id, null!));
    }
}
