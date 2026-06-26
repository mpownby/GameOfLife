using GameOfLife.Api.Data;
using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Integration.Test;

/// <summary>
/// Drives <see cref="BoardRepositoryUsingFileSystem"/> with the real <see cref="FileIOPassThrough"/>
/// against a throwaway temp directory. This is the end-to-end proof of the "survive restart/crash
/// and retain the state of the boards" requirement — it actually hits the disk, so it is an
/// integration test rather than a unit test. (The pass-through itself has no logic to unit-test.)
/// </summary>
[TestFixture]
public class BoardRepositoryIntegrationTests
{
    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(Path.GetTempPath(), $"gol_test_{Guid.NewGuid():N}", "boards.json");
    }

    [TearDown]
    public void TearDown()
    {
        var directory = Path.GetDirectoryName(_path)!;
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static BoardState StateWith(params Cell[] cells) => new()
    {
        InitialState = new HashSet<Cell>(cells),
        CurrentState = new HashSet<Cell>(cells),
        IterationCount = 0
    };

    [Test]
    public void CreatedBoard_SurvivesRestart()
    {
        // First "process": create a board and let it persist to disk.
        var repository = new BoardRepositoryUsingFileSystem(new FileIOPassThrough(), _path);
        var id = repository.CreateNewBoard(StateWith(new Cell(2, 3), new Cell(4, 5)));

        // Second "process": a brand-new repository pointed at the same file must reload it.
        var afterRestart = new BoardRepositoryUsingFileSystem(new FileIOPassThrough(), _path);
        var board = afterRestart.GetExistingBoard(id);

        Assert.That(board.InitialState, Is.EquivalentTo(new[] { new Cell(2, 3), new Cell(4, 5) }));
        Assert.That(board.CurrentState, Is.EquivalentTo(new[] { new Cell(2, 3), new Cell(4, 5) }));
    }

    [Test]
    public void Updates_ArePersistedAcrossRestart()
    {
        var repository = new BoardRepositoryUsingFileSystem(new FileIOPassThrough(), _path);
        var id = repository.CreateNewBoard(StateWith(new Cell(0, 0)));

        var updated = StateWith(new Cell(1, 1));
        updated.IterationCount = 5;
        repository.UpdateExistingBoard(id, updated);

        var afterRestart = new BoardRepositoryUsingFileSystem(new FileIOPassThrough(), _path);
        var board = afterRestart.GetExistingBoard(id);

        Assert.That(board.CurrentState, Is.EquivalentTo(new[] { new Cell(1, 1) }));
        Assert.That(board.IterationCount, Is.EqualTo(5));
    }
}
