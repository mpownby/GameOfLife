using GameOfLife.Api.Data;

namespace GameOfLife.Api.Service;

/// <summary>
/// Implementation of <see cref="IBoardService"/>. Skeleton only: the rules engine
/// and orchestration to be written. Depends on <see cref="IBoardRepository"/>
/// (the interface, not a concrete store) for persistence.
/// </summary>
public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;

    public BoardService(IBoardRepository boardRepository)
    {
        _boardRepository = boardRepository;
    }
}
