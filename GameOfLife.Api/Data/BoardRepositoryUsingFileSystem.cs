using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Data;

/// <summary>
/// File-backed implementation of <see cref="IBoardRepository"/>.
/// </summary>
public class BoardRepositoryUsingFileSystem : IBoardRepository
{
    public int CreateNewBoard(BoardState src)
    {
        throw new NotImplementedException();
    }

    public BoardState GetExistingBoard(int id)
    {
        throw new NotImplementedException();
    }

    public void UpdateExistingBoard(int id, BoardState state)
    {
        throw new NotImplementedException();
    }
}
