namespace GameOfLife.Api.Web;

/// <summary>
/// Sparse upload shape: only the live cells, each as an [x, y] pair. This is the
/// primary upload format because it scales to very large/sparse boards.
/// Example body: { "liveCells": [ [0, 0], [1, 0], [2, 0] ] }
/// </summary>
public class CoordinateBoardRequest
{
    public long[][] LiveCells { get; set; } = [];
}
