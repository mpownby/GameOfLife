using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Service;

/// <summary>
/// Implementation of <see cref="IBoardStepper"/>: the concrete Game of Life stepping rule.
/// </summary>
public class BoardStepper : IBoardStepper
{
    public HashSet<Cell> Step(IReadOnlySet<Cell> liveCells)
    {
        var result = new HashSet<Cell>();   // what we return

        // tracks the count of all neighbors of cells
        var dictNeighborCount = new Dictionary<Cell, int>();

        // create the dictionary
        foreach (Cell cell in liveCells)
        {
            StepHelper(liveCells, cell, dictNeighborCount);
        }

        // now create the new cell set based on the dictionary counts
        foreach (var pair in dictNeighborCount)
        {
            // a cell with 3 neighbors always will be alive whether previously alive or dead
            if (pair.Value == 3)
            {
                result.Add(pair.Key);
            }
            // else if the cell has 2 neighors and was previously alive, it will still be alive
            else if ((pair.Value == 2) && liveCells.Contains(pair.Key))
            {
                result.Add(pair.Key);
            }
            // else it is dead or dies
        }

        return result;
    }

    private void StepHelper(IReadOnlySet<Cell> liveCells, Cell cell, Dictionary<Cell, int> dictNeighborCount)
    {
        int countNeighbors = 0;

        if (liveCells.Contains(new Cell(cell.X - 1, cell.Y))) countNeighbors++; // left
        if (liveCells.Contains(new Cell(cell.X - 1, cell.Y - 1))) countNeighbors++; // upper-left
        if (liveCells.Contains(new Cell(cell.X, cell.Y - 1))) countNeighbors++; // top
        if (liveCells.Contains(new Cell(cell.X + 1, cell.Y - 1))) countNeighbors++; // upper-right
        if (liveCells.Contains(new Cell(cell.X + 1, cell.Y))) countNeighbors++; // right
        if (liveCells.Contains(new Cell(cell.X + 1, cell.Y + 1))) countNeighbors++; // lower-right
        if (liveCells.Contains(new Cell(cell.X, cell.Y + 1))) countNeighbors++; // bottom
        if (liveCells.Contains(new Cell(cell.X - 1, cell.Y + 1))) countNeighbors++; // lower-left

        if (countNeighbors > 0)
        {
            dictNeighborCount[cell] = countNeighbors;
        }
    }
}
