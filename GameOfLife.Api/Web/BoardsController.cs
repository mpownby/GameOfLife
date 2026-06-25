using GameOfLife.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.Api.Web;

/// <summary>
/// HTTP surface for boards. Depends only on <see cref="IBoardService"/> (the
/// interface), keeping all rules/persistence concerns out of the Web layer.
/// Endpoint bodies are skeletons returning 501 until the service is implemented.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardsController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    /// <summary>
    /// Upload a new board as a sparse list of live cell coordinates. Returns the id of the new board.
    /// </summary>
    [HttpPost]
    public ActionResult CreateBoard([FromBody] CoordinateBoardRequest request)
    {
        // TODO: validate request, map request.LiveCells (long[][]) -> IReadOnlyCollection<Cell>,
        // call _boardService.CreateNewBoard(cells), then
        //   return CreatedAtAction(nameof(GetBoard), new { id }, new { id });
        return StatusCode(StatusCodes.Status501NotImplemented, new { error = "Not implemented yet" });
    }

    /// <summary>
    /// Convenience: upload a new board as a small dense grid (e.g. a 2x2 block). Returns the id of the new board.
    /// </summary>
    [HttpPost("from-grid")]
    public ActionResult CreateBoardFromGrid([FromBody] GridBoardRequest request)
    {
        // TODO: cells = _boardService.ConvertGridToLiveCells(request.Grid), then
        // _boardService.CreateNewBoard(cells) — same create/persist flow as CreateBoard.
        return StatusCode(StatusCodes.Status501NotImplemented, new { error = "Not implemented yet" });
    }
}
