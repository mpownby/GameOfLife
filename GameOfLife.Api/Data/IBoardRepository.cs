using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Data;

/// <summary>
/// Data layer: persistence of boards behind an abstraction, so the backing store
/// (a file on disk now, a database later) can be swapped without touching the
/// service layer. Methods to be designed (e.g. save board, load board by id).
/// </summary>
public interface IBoardRepository
{
    int CreateNewBoard(BoardState src);

    /// <summary>
    /// Load the board with the given id.
    /// </summary>
    /// <remarks>
    /// CONTRACT: the returned <see cref="BoardState"/> is owned by the caller and is safe to mutate
    /// freely. Implementations MUST return an instance that is not shared with any internal cache or
    /// other caller (e.g. a fresh deserialization or a defensive copy), so that mutating it cannot
    /// corrupt stored state or affect concurrent callers. The service layer relies on this to advance
    /// a board in place before persisting it back via <see cref="UpdateExistingBoard"/>.
    /// </remarks>
    /// <param name="id">The id of the board to load.</param>
    /// <returns>A caller-owned, freely-mutable snapshot of the board's state.</returns>
    BoardState GetExistingBoard(int id);

    void UpdateExistingBoard(int id, BoardState state);
}
