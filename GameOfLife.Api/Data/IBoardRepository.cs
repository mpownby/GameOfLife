namespace GameOfLife.Api.Data;

/// <summary>
/// Data layer: persistence of boards behind an abstraction, so the backing store
/// (a file on disk now, a database later) can be swapped without touching the
/// service layer. Methods to be designed (e.g. save board, load board by id).
/// </summary>
public interface IBoardRepository
{
}
