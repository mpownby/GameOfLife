using System.Text.Json;
using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Data;

/// <summary>
/// Repository that keeps boards in an in-memory list (the list index is the board id) and
/// persists the whole list as JSON to a file. Reads are served entirely from memory; the file
/// is only touched on construction (initial load) and on each write. All access is guarded by a
/// lock because the repository is a singleton shared across concurrent requests.
/// </summary>
public class BoardRepositoryUsingFileSystem : IBoardRepository
{
    // thin pass-through over System.IO statics, so the file logic in this class can be unit-tested
    private readonly IFileIOPassThrough _fileIO;

    // where the serialized catalog lives on disk
    private readonly string _filePath;

    // arbitrary object to handle locking
    private readonly object _lock = new();

    // cache of all boards that have been created; the id is the index within the list
    private readonly List<BoardState> _boards;

    public BoardRepositoryUsingFileSystem(IFileIOPassThrough fileIO, string filePath)
    {
        ArgumentNullException.ThrowIfNull(fileIO);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _fileIO = fileIO;
        _filePath = filePath;

        // make sure the target directory exists so writes won't fail later
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            _fileIO.CreateDirectory(directory);
        }

        // here's how we resume in the event of a crash; load cached data from a previous session (if it exists)
        var json = _fileIO.Exists(_filePath) ? _fileIO.ReadAllText(_filePath) : null;
        _boards = string.IsNullOrEmpty(json)
            ? []
            : JsonSerializer.Deserialize<List<BoardState>>(json) ?? [];
    }

    public int CreateNewBoard(BoardState src)
    {
        // sanity check
        ArgumentNullException.ThrowIfNull(src);

        // locking to prevent race conditions
        lock (_lock)
        {
            _boards.Add(Clone(src));
            int id = _boards.Count - 1;
            Persist();
            return id;
        }
    }

    public BoardState GetExistingBoard(int id)
    {
        // locking to prevent race conditions
        lock (_lock)
        {
            return Clone(GetOrThrow(id));
        }
    }

    public void UpdateExistingBoard(int id, BoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // locking to prevent race conditions
        lock (_lock)
        {
            GetOrThrow(id);             // validates the id exists before replacing
            _boards[id] = Clone(state);
            Persist();
        }
    }

    private BoardState GetOrThrow(int id)
    {
        // range check
        if (id < 0 || id >= _boards.Count)
        {
            throw new KeyNotFoundException($"No board exists with id {id}.");
        }

        return _boards[id];
    }

    private void Persist()
    {
        // atomic write: write to a sibling temp file then move it over the target, so a crash
        // mid-write can't corrupt the existing catalog
        var tempPath = _filePath + ".tmp";
        _fileIO.WriteAllText(tempPath, JsonSerializer.Serialize(_boards));
        _fileIO.Move(tempPath, _filePath, overwrite: true);
    }

    /// <summary>
    /// Copies the BoardState so that we are decoupled from the caller.  If they change the hashsets, our values won't be affected.
    /// </summary>
    /// <param name="src">The BoardState we want to clone</param>
    /// <returns>The cloned BoardState</returns>
    private static BoardState Clone(BoardState src) => new()
    {
        InitialState = new HashSet<Cell>(src.InitialState),
        CurrentState = new HashSet<Cell>(src.CurrentState),
        IterationCount = src.IterationCount
    };
}
