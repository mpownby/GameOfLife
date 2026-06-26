using GameOfLife.Api.Data;

namespace GameOfLife.Integration.Test;

/// <summary>
/// Exercises the real file I/O of <see cref="FileBoardStateStore"/> against a throwaway
/// temp directory. These touch the filesystem, so they are integration tests, not unit tests.
/// They prove the durability requirement actually holds.
/// </summary>
[TestFixture]
public class FileBoardStateStoreTests
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

    [Test]
    public void Load_NoFileYet_ReturnsNull()
    {
        var store = new FileBoardStateStore(_path);

        Assert.That(store.Load(), Is.Null);
    }

    [Test]
    public void SaveThenLoad_RoundTripsContents()
    {
        var store = new FileBoardStateStore(_path);

        store.Save("hello world");

        Assert.That(store.Load(), Is.EqualTo("hello world"));
    }

    [Test]
    public void Save_OverwritesPreviousContents()
    {
        var store = new FileBoardStateStore(_path);

        store.Save("first");
        store.Save("second");

        Assert.That(store.Load(), Is.EqualTo("second"));
    }

    [Test]
    public void Load_FromASecondInstance_SeesPersistedContents()
    {
        new FileBoardStateStore(_path).Save("durable");

        // Simulate a restart: a fresh instance pointed at the same path.
        var afterRestart = new FileBoardStateStore(_path);

        Assert.That(afterRestart.Load(), Is.EqualTo("durable"));
    }
}
