using GameOfLife.Api.Data.Objects;
using GameOfLife.Api.Service;
using GameOfLife.Api.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute.ExceptionExtensions;

namespace GameOfLife.Unit.Test;

[TestFixture]
public class BoardsControllerTests
{
    private IBoardService _service = null!;
    private BoardsController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = Substitute.For<IBoardService>();
        _controller = new BoardsController(_service);
    }

    // ---------------------------------------------------------------------
    // CreateBoard (sparse coordinates)
    // ---------------------------------------------------------------------

    [Test]
    public void CreateBoard_MapsCoordinatePairsToCellsAndReturnsCreatedWithId()
    {
        var request = new CoordinateBoardRequest { LiveCells = [[0, 0], [1, 2]] };
        IReadOnlyCollection<Cell>? passedCells = null;
        _service.CreateNewBoard(Arg.Do<IReadOnlyCollection<Cell>>(c => passedCells = c)).Returns(5);

        var result = _controller.CreateBoard(request);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.ActionName, Is.EqualTo(nameof(BoardsController.GetBoard)));
        Assert.That(created.RouteValues!["id"], Is.EqualTo(5));
        Assert.That(passedCells, Is.EquivalentTo(new[] { new Cell(0, 0), new Cell(1, 2) }));
    }

    [Test]
    public void CreateBoard_EmptyLiveCells_CreatesEmptyBoard()
    {
        var request = new CoordinateBoardRequest { LiveCells = [] };
        IReadOnlyCollection<Cell>? passedCells = null;
        _service.CreateNewBoard(Arg.Do<IReadOnlyCollection<Cell>>(c => passedCells = c)).Returns(9);

        var result = _controller.CreateBoard(request);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.RouteValues!["id"], Is.EqualTo(9));
        Assert.That(passedCells, Is.Empty);
    }

    [Test]
    public void CreateBoard_MalformedPair_ReturnsBadRequestAndDoesNotCreate()
    {
        // Second pair has only one element, so it is not a valid [x, y] coordinate.
        var request = new CoordinateBoardRequest { LiveCells = [[0, 0], [1]] };

        var result = _controller.CreateBoard(request);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _service.DidNotReceive().CreateNewBoard(Arg.Any<IReadOnlyCollection<Cell>>());
    }

    // ---------------------------------------------------------------------
    // CreateBoardFromGrid (convenience dense grid)
    // ---------------------------------------------------------------------

    [Test]
    public void CreateBoardFromGrid_ConvertsGridThenCreatesAndReturnsCreatedWithId()
    {
        bool[][] grid = [[true, false], [false, true]];
        var request = new GridBoardRequest { Grid = grid };
        Cell[] converted = [new Cell(0, 0), new Cell(1, 1)];
        _service.ConvertGridToLiveCells(grid).Returns(converted);
        _service.CreateNewBoard(converted).Returns(11);

        var result = _controller.CreateBoardFromGrid(request);

        var created = result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.ActionName, Is.EqualTo(nameof(BoardsController.GetBoard)));
        Assert.That(created.RouteValues!["id"], Is.EqualTo(11));
        _service.Received(1).ConvertGridToLiveCells(grid);
        _service.Received(1).CreateNewBoard(converted);
    }

    // ---------------------------------------------------------------------
    // GetBoard / GetNextState / GetStateAfterSteps — read & step endpoints
    // ---------------------------------------------------------------------

    [Test]
    public void GetBoard_ExistingBoard_ReturnsOkWithState()
    {
        var state = new BoardState { IterationCount = 7 };
        _service.IterateNSteps(3, 0).Returns(state);

        var result = _controller.GetBoard(3);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(state));
    }

    [Test]
    public void GetBoard_UnknownId_ReturnsNotFound()
    {
        _service.IterateNSteps(99, 0).Throws(new KeyNotFoundException("No board exists with id 99."));

        var result = _controller.GetBoard(99);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetNextState_AdvancesOneStepAndReturnsOk()
    {
        var state = new BoardState { IterationCount = 1 };
        _service.IterateNSteps(4, 1).Returns(state);

        var result = _controller.GetNextState(4);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(state));
        _service.Received(1).IterateNSteps(4, 1);
    }

    [Test]
    public void GetNextState_UnknownId_ReturnsNotFound()
    {
        _service.IterateNSteps(99, 1).Throws(new KeyNotFoundException("No board exists with id 99."));

        var result = _controller.GetNextState(99);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetStateAfterSteps_AdvancesRequestedStepsAndReturnsOk()
    {
        var state = new BoardState { IterationCount = 10 };
        _service.IterateNSteps(2, 10).Returns(state);

        var result = _controller.GetStateAfterSteps(2, 10);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(state));
        _service.Received(1).IterateNSteps(2, 10);
    }

    [Test]
    public void GetStateAfterSteps_UnknownId_ReturnsNotFound()
    {
        _service.IterateNSteps(99, 5).Throws(new KeyNotFoundException("No board exists with id 99."));

        var result = _controller.GetStateAfterSteps(99, 5);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetStateAfterSteps_NegativeSteps_ReturnsBadRequest()
    {
        _service.IterateNSteps(1, -1).Throws(new ArgumentOutOfRangeException("iterationCount"));

        var result = _controller.GetStateAfterSteps(1, -1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ---------------------------------------------------------------------
    // FinalizeBoard — settle-or-fail endpoint
    // ---------------------------------------------------------------------

    [Test]
    public void FinalizeBoard_SettlesAndReturnsOk()
    {
        var state = new BoardState { IterationCount = 12 };
        _service.FinalizeBoard(6, 1000).Returns(state);

        var result = _controller.FinalizeBoard(6);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.SameAs(state));
    }

    [Test]
    public void FinalizeBoard_UnknownId_ReturnsNotFound()
    {
        _service.FinalizeBoard(99, Arg.Any<int>())
            .Throws(new KeyNotFoundException("No board exists with id 99."));

        var result = _controller.FinalizeBoard(99);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void FinalizeBoard_DidNotConclude_ReturnsUnprocessableEntity()
    {
        _service.FinalizeBoard(8, 50).Throws(new BoardDidNotConcludeException(8, 50));

        var result = _controller.FinalizeBoard(8, 50);

        // 422 Unprocessable Entity: the request was well-formed but the board never settled.
        var unprocessable = result as UnprocessableEntityObjectResult;
        Assert.That(unprocessable, Is.Not.Null);
        Assert.That(unprocessable!.StatusCode, Is.EqualTo(StatusCodes.Status422UnprocessableEntity));
    }

    [Test]
    public void FinalizeBoard_NegativeBudget_ReturnsBadRequest()
    {
        _service.FinalizeBoard(1, -5).Throws(new ArgumentOutOfRangeException("maxIterationCount"));

        var result = _controller.FinalizeBoard(1, -5);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}
