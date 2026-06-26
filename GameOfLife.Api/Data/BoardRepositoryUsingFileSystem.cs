using System.Text.Json;
using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Data;

/// <summary>
/// Repository that keeps boards in an in-memory list (the list index is the board id) and
/// persists the whole list as JSON via an <see cref="IBoardStateStore"/>. Reads are served
/// entirely from memory; the store is only touched on construction (initial load) and on
/// each write. All access is guarded by a lock because the repository is a singleton shared
/// across concurrent requests.
/// </summary>
public class BoardRepositoryUsingFileSystem : IBoardRepository
{
    private readonly IBoardStateStore _store;
    private readonly object _lock = new();
    private readonly List<BoardState> _boards;

    public BoardRepositoryUsingFileSystem(IBoardStateStore store)
    {
        _store = store;

        var json = _store.Load();
        _boards = string.IsNullOrEmpty(json)
            ? []
            : JsonSerializer.Deserialize<List<BoardState>>(json) ?? [];
    }

    public int CreateNewBoard(BoardState src)
    {
        ArgumentNullException.ThrowIfNull(src);

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
        lock (_lock)
        {
            return Clone(GetOrThrow(id));
        }
    }

    public void UpdateExistingBoard(int id, BoardState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_lock)
        {
            GetOrThrow(id);             // validates the id exists before replacing
            _boards[id] = Clone(state);
            Persist();
        }
    }

    private BoardState GetOrThrow(int id)
    {
        if (id < 0 || id >= _boards.Count)
        {
            throw new KeyNotFoundException($"No board exists with id {id}.");
        }

        return _boards[id];
    }

    private void Persist()
    {
        _store.Save(JsonSerializer.Serialize(_boards));
    }

    // Independent copy so the cached board can only ever change via Update (under the lock),
    // never through a reference a caller is still holding. Cell is a value type, so copying
    // the HashSets copies the cells too.
    private static BoardState Clone(BoardState src) => new()
    {
        InitialState = new HashSet<Cell>(src.InitialState),
        CurrentState = new HashSet<Cell>(src.CurrentState),
        IterationCount = src.IterationCount
    };
}
