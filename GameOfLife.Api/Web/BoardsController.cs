using GameOfLife.Api.Data.Objects;
using GameOfLife.Api.Service;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace GameOfLife.Api.Web;

/// <summary>
/// HTTP surface for boards. Depends only on <see cref="IBoardService"/> (the interface),
/// keeping all rules/persistence concerns out of the Web layer. The Web layer owns the
/// mapping between wire-format request DTOs and the service's domain types, and between the
/// service's domain exceptions and HTTP status codes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardsController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Upload a new board (sparse).",
        Description = "Takes the live cells as [x, y] coordinate pairs — the format that scales to large, sparse boards. Returns the new board's id; the Location header points at its resource.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Board created.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "A live cell was not a valid [x, y] pair.")]
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

    [HttpPost("from-grid")]
    [SwaggerOperation(
        Summary = "Upload a new board (dense grid convenience).",
        Description = "Takes a small dense, row-major grid (true = alive) where grid[row][col] maps to (x = col, y = row). Intended for hand-written fixtures like a 2x2 block, not large boards. Returns the new board's id.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Board created.")]
    public ActionResult CreateBoardFromGrid([FromBody] GridBoardRequest request)
    {
        int id = _boardService.CreateNewBoard(_boardService.ConvertGridToLiveCells(request.Grid));

        return CreatedAtAction(nameof(GetBoard), new { id }, new { id });
    }

    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get a board's current state.",
        Description = "The canonical resource location that the create endpoints point their Location header at.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The board's current state.", typeof(BoardState))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No board exists with the given id.")]
    public ActionResult GetBoard(int id)
        => Run(() => _boardService.IterateNSteps(id, 0));

    [HttpGet("{id}/next")]
    [SwaggerOperation(
        Summary = "Advance a board one step.",
        Description = "Steps the board a single iteration, persists the advanced state, and returns it.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The board's state after one iteration.", typeof(BoardState))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No board exists with the given id.")]
    public ActionResult GetNextState(int id)
        => Run(() => _boardService.IterateNSteps(id, 1));

    [HttpGet("{id}/states/{steps}")]
    [SwaggerOperation(
        Summary = "Advance a board N steps.",
        Description = "Steps the board the requested number of iterations, persists the advanced state, and returns it. 0 returns the current state without advancing.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The board's state after the requested number of iterations.", typeof(BoardState))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The step count was negative.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No board exists with the given id.")]
    public ActionResult GetStateAfterSteps(
        int id,
        [SwaggerParameter("How many iterations to advance. Must be non-negative.")] int steps)
        => Run(() => _boardService.IterateNSteps(id, steps));

    [HttpGet("{id}/final")]
    [SwaggerOperation(
        Summary = "Drive a board to its final state.",
        Description = "Steps until the board settles — a still life or extinction, where the next state equals the current one — and returns the settled state. If it has not settled within the iteration budget, returns 422 rather than running unbounded.")]
    [SwaggerResponse(StatusCodes.Status200OK, "The board's final, settled state.", typeof(BoardState))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The iteration budget was negative.")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No board exists with the given id.")]
    [SwaggerResponse(StatusCodes.Status422UnprocessableEntity, "The board did not settle within the iteration budget.")]
    public ActionResult FinalizeBoard(
        int id,
        [FromQuery]
        [SwaggerParameter("The iteration budget before giving up. Must be non-negative.")] int maxIterations = 1000)
        => Run(() => _boardService.FinalizeBoard(id, maxIterations));

    /// <summary>
    /// Invoke a board-returning service call and translate the service layer's domain exceptions into
    /// HTTP status codes. Centralizing this keeps every read/step/finalize endpoint a one-liner and
    /// gives them identical, consistent error mapping. The mapping lives here (not in middleware) so it
    /// is exercised directly by the controller's unit tests.
    /// </summary>
    private ActionResult Run(Func<BoardState> serviceCall)
    {
        try
        {
            return Ok(serviceCall());
        }
        catch (KeyNotFoundException ex)
        {
            // Unknown board id — the brief's "invalid id" case.
            return NotFound(new { error = ex.Message });
        }
        catch (BoardDidNotConcludeException ex)
        {
            // The board never settled within the caller's budget. 422 (Unprocessable Entity): the
            // request was well-formed but we cannot produce the requested result.
            return UnprocessableEntity(new { error = ex.Message, ex.BoardId, ex.MaxIterationCount });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // A negative step/iteration count is a client mistake, not a server fault.
            return BadRequest(new { error = ex.Message });
        }
    }
}
