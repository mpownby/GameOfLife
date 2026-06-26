using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Service;

/// <summary>
/// Owns the single-generation Game of Life rule: given the current live cells,
/// produce the live cells of the next generation. Lives behind its own interface
/// because the stepping logic has multiple branches (neighbour counting, birth/survival
/// rules, boundary handling) and both <see cref="IBoardService.IterateNSteps"/>
/// and <see cref="IBoardService.FinalizeBoard"/> depend on it.
/// </summary>
public interface IBoardStepper
{
    /// <summary>
    /// Advance the board by exactly one generation.
    /// </summary>
    /// <param name="liveCells">The currently live cells. Not mutated.</param>
    /// <returns>The live cells of the next generation.</returns>
    HashSet<Cell> Step(IReadOnlySet<Cell> liveCells);
}
