using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Service;

/// <summary>
/// Service layer: owns the Game of Life rules and orchestration (create a board,
/// step to the next state, jump N states, compute the final state). Speaks only in
/// domain types (<see cref="Cell"/>, <see cref="BoardState"/>) — never in web request
/// shapes — so the Web layer can map its DTOs to these and the service stays decoupled
/// from how data arrives over HTTP.
/// </summary>
public interface IBoardService
{
    /// <summary>
    /// Create a new board from a set of live cells.
    /// </summary>
    /// <param name="liveCells">The live cells the board starts with.</param>
    /// <returns>The id of the created board.</returns>
    int CreateNewBoard(IReadOnlyCollection<Cell> liveCells);

    /// <summary>
    /// Convert a dense, row-major grid (true = alive) into the live-cell set used everywhere
    /// else. grid[row][col] maps to (x = col, y = row). Pure transform; does not create a board.
    /// </summary>
    /// <param name="grid">The dense grid to convert.</param>
    /// <returns>The live cells the grid represents.</returns>
    IReadOnlyCollection<Cell> ConvertGridToLiveCells(bool[][] grid);

    /// <summary>
    /// Advance the board by iterationCount and return the new state.
    /// Passing in 0 iterations will just return the current state.
    /// </summary>
    /// <param name="id">The id of the board.</param>
    /// <param name="iterationCount">How many iterations to advance the board.</param>
    /// <returns>The board state after it has advanced.</returns>
    BoardState GetStateAfterIterations(int id, int iterationCount);

    /// <summary>
    /// Drives the board to a final state.
    /// If board doesn't settle into a final state before maxIterationCount, throw exception.
    /// </summary>
    /// <param name="id">The id of the board.</param>
    /// <param name="maxIterationCount">Max iterations we will run before giving up and throwing.</param>
    /// <returns>The final state of the board.</returns>
    BoardState FinalizeBoard(int id, int maxIterationCount);
}
