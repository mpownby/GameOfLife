using GameOfLife.Api.Data.Objects;
using GameOfLife.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.Api.Web;

/// <summary>
/// HTTP surface for boards. Depends only on <see cref="IBoardService"/> (the interface),
/// keeping all rules/persistence concerns out of the Web layer. The Web layer owns the
/// mapping between wire-format request DTOs and the service's domain types.
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
    /// Upload a new board as a sparse list of live cell [x, y] coordinates. Returns the id of the new board.
    /// </summary>
    [HttpPost]
    public ActionResult CreateBoard([FromBody] CoordinateBoardRequest request)
    {
        // Map the wire format ([x, y] pairs) to the service's domain type inline.
        var liveCells = new List<Cell>(request.LiveCells.Length);
        foreach (var pair in request.LiveCells)
        {
            if (pair.Length != 2)
            {
                return BadRequest(new { error = "Each live cell must be an [x, y] pair." });
            }

            liveCells.Add(new Cell(pair[0], pair[1]));
        }

        int id = _boardService.CreateNewBoard(liveCells);

        return CreatedAtAction(nameof(GetBoard), new { id }, new { id });
    }

    /// <summary>
    /// Convenience: upload a new board as a small dense grid (e.g. a 2x2 block). Returns the id of the new board.
    /// </summary>
    [HttpPost("from-grid")]
    public ActionResult CreateBoardFromGrid([FromBody] GridBoardRequest request)
    {
        int id = _boardService.CreateNewBoard(_boardService.ConvertGridToLiveCells(request.Grid));

        return CreatedAtAction(nameof(GetBoard), new { id }, new { id });
    }

    /// <summary>
    /// Get the current state of a board by id. This is the canonical resource location that the
    /// create endpoints point their Location header at.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult GetBoard(int id)
    {
        BoardState state = _boardService.GetStateAfterIterations(id, 0);
        return Ok(state);
    }
}
