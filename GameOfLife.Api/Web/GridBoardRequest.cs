namespace GameOfLife.Api.Web;

/// <summary>
/// Convenience upload shape: a small dense grid, row-major, where true = alive.
/// grid[row][col] maps to coordinate (x = col, y = row), origin at top-left (0, 0).
/// Intended for hand-written test fixtures (e.g. a 2x2 block) where listing raw
/// coordinates is awkward. Not for large boards.
/// Example body (2x2 block): { "grid": [ [true, true], [true, true] ] }
/// </summary>
public class GridBoardRequest
{
    public bool[][] Grid { get; set; } = [];
}
