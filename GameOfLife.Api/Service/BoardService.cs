using GameOfLife.Api.Data;
using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Service;

/// <summary>
/// Implementation of <see cref="IBoardService"/>. Depends on <see cref="IBoardRepository"/>
/// (the interface, not a concrete store) for persistence.
/// </summary>
public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;

    public BoardService(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }

    public int CreateNewBoard(IReadOnlyCollection<Cell> liveCells)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<Cell> ConvertGridToLiveCells(bool[][] grid)
    {
        throw new NotImplementedException();
    }

    public BoardState GetStateAfterIterations(int id, int iterationCount)
    {
        throw new NotImplementedException();
    }

    public BoardState FinalizeBoard(int id, int maxIterationCount)
    {
        throw new NotImplementedException();
    }
}
