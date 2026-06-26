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

        // live-neighbor count for every cell adjacent to at least one live cell
        var dictNeighborCount = new Dictionary<Cell, int>();

        // create the dictionary
        //
        // The coordinate math below is deliberately `unchecked`: at the extreme edges of the signed
        // 64-bit space, cell.X + 1 (or - 1) overflows, and we WANT it to wrap (long.MaxValue and
        // long.MinValue are treated as adjacent — a torus) rather than throw. Default C# arithmetic
        // is already unchecked, but stating it explicitly makes the intent local and keeps the
        // behavior correct even if the build is ever switched to checked arithmetic project-wide.
        // (See the wraparound test in BoardStepperTests.)
        unchecked
        {
            foreach (Cell cell in liveCells)
            {
                IncrementNeighborCount(new Cell(cell.X - 1, cell.Y), dictNeighborCount);    // left
                IncrementNeighborCount(new Cell(cell.X - 1, cell.Y - 1), dictNeighborCount);    // upper-left
                IncrementNeighborCount(new Cell(cell.X, cell.Y - 1), dictNeighborCount);    // top
                IncrementNeighborCount(new Cell(cell.X + 1, cell.Y - 1), dictNeighborCount);    // upper-right
                IncrementNeighborCount(new Cell(cell.X + 1, cell.Y), dictNeighborCount);    // right
                IncrementNeighborCount(new Cell(cell.X + 1, cell.Y + 1), dictNeighborCount);    // lower-right
                IncrementNeighborCount(new Cell(cell.X, cell.Y + 1), dictNeighborCount);    // bottom
                IncrementNeighborCount(new Cell(cell.X - 1, cell.Y + 1), dictNeighborCount);    // lower-left
            }
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

    private void IncrementNeighborCount(Cell cell, Dictionary<Cell, int> dictNeighborCount)
    {
        // GetValueOrDefault returns 0 for a key that isn't present yet, so this both
        // seeds and increments in one line.
        // A faster alternative is CollectionsMarshal.GetValueRefOrAddDefault(dictNeighborCount, cell, out _)++;
        // which hashes the key only once instead of twice. I find it less readable, so I've
        // kept the version below; if performance becomes crucial, I would switch to it.
        dictNeighborCount[cell] = dictNeighborCount.GetValueOrDefault(cell) + 1;
    }
}
