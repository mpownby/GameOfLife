namespace GameOfLife.Api.Data;

/// <summary>
/// File-backed <see cref="IBoardStateStore"/>. Reads/writes a single file. Writes are
/// atomic (write a temp file then move it over the target) so a crash mid-write cannot
/// corrupt the existing catalog.
/// </summary>
public class FileBoardStateStore : IBoardStateStore
{
    private readonly string _filePath;

    public FileBoardStateStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public string? Load()
    {
        return File.Exists(_filePath) ? File.ReadAllText(_filePath) : null;
    }

    public void Save(string contents)
    {
        // Write to a sibling temp file first, then atomically replace the target. If the
        // process dies before the move, the original file is left intact.
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, contents);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
