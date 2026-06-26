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
    private readonly IBoardStepper _boardStepper;

    public BoardService(IBoardRepository boardRepository, IBoardStepper boardStepper)
    {
        _boardRepository = boardRepository;
        _boardStepper = boardStepper;
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

    public BoardState IterateNSteps(int id, int iterationCount)
    {
        // Fail fast on a nonsensical request before doing any I/O. 0 is valid (returns the
        // current state); a negative count almost certainly signals a caller bug.
        ArgumentOutOfRangeException.ThrowIfNegative(iterationCount);

        BoardState state = this._boardRepository.GetExistingBoard(id);

        int iterations = 0;

        // using 'while' here instead of 'for' to leave room for future cycle detection which could drastically optimize this method if a large iteration count is supplied.
        // (currently out of scope)
        while (iterations < iterationCount)
        {
            state.CurrentState = this._boardStepper.Step(state.CurrentState);
            iterations++;
        }

        state.IterationCount += iterations;

        // Persist only when we actually advanced the board. 0 iterations is a quirky-but-valid
        // way to just read the current state, so it does no work and writes nothing.
        if (iterations > 0)
        {
            this._boardRepository.UpdateExistingBoard(id, state);
        }

        return state;
    }

    public BoardState FinalizeBoard(int id, int maxIterationCount)
    {
        // A negative budget is a caller bug; fail fast before any I/O. (0 is permitted: it means
        // "don't even try", and falls through to the did-not-conclude path below.)
        ArgumentOutOfRangeException.ThrowIfNegative(maxIterationCount);

        BoardState state = this._boardRepository.GetExistingBoard(id);

        int iterations = 0;

        while (iterations < maxIterationCount)
        {
            HashSet<Cell> setNew = this._boardStepper.Step(state.CurrentState);

            iterations++;

            // we want to persist the changes, but for performance reasons we'll only persist right before returning or throwing

            // if the set hasn't changed, it means we've reached conclusion
            if (setNew.SetEquals(state.CurrentState))
            {
                state.IterationCount += iterations;
                this._boardRepository.UpdateExistingBoard(id, state);
                return state;
            }

            state.CurrentState = setNew;

            // TODO : add cycle detection to fail faster
        }

        // if we get this far, it means that we reached no conclusion by the caller's criteria, so we fail

        // no need to persist if we didn't change any state
        if (iterations > 0)
        {
            state.IterationCount += iterations;
            this._boardRepository.UpdateExistingBoard(id, state);   // we want to persist in case the caller decides to retry finalization later with a more permissive threshold
        }

        throw new BoardDidNotConcludeException(id, maxIterationCount);
    }
}
