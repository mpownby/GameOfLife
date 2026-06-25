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
        ArgumentNullException.ThrowIfNull(liveCells);

        var state = new BoardState
        {
            InitialState = new HashSet<Cell>(liveCells),
            CurrentState = new HashSet<Cell>(liveCells),    // with 0 iterations, current state matches initial state
            IterationCount = 0
        };

        return _boardRepository.CreateNewBoard(state);
    }

    public IReadOnlyCollection<Cell> ConvertGridToLiveCells(bool[][] grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

        List<Cell> result = [];

        for (int row = 0; row < grid.Length; row++)
        {
            // Bound by THIS row's length so jagged grids are handled correctly
            // (rather than assuming every row is as wide as grid[0]).
            bool[]? gridRow = grid[row];
            if (gridRow == null)
            {
                throw new ArgumentException("Grid row cannot be null.");
            }

            for (int col = 0; col < gridRow.Length; col++)
            {
                if (gridRow[col])
                {
                    result.Add(new Cell(col, row));
                }
            }
        }

        return result;
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
