using GameOfLife.Api.Data.Objects;
using GameOfLife.Api.Service;
using GameOfLife.Api.Web;
using Microsoft.AspNetCore.Mvc;

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
}
