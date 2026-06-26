using GameOfLife.Api.Data.Objects;
using GameOfLife.Api.Service;

namespace GameOfLife.Unit.Test
{
    [TestFixture]
    public class BoardStepperTests
    {
        private BoardStepper _service = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new BoardStepper();
        }

        #region StillLife

        // A still life is a pattern that is unchanged after one step.
        [TestCaseSource(nameof(StillLifePatterns))]
        public void StillLife_IsUnchanged(Cell[] cells)
        {
            var cellsSrc = new HashSet<Cell>(cells);

            HashSet<Cell> actual = _service.Step(cellsSrc);

            Assert.That(actual.SetEquals(cellsSrc), Is.True);
        }

        private static readonly TestCaseData[] StillLifePatterns =
        [
            // block:
            //   X X
            //   X X
            new TestCaseData((object)new Cell[]
            {
                new(0, 0), new(1, 0),
                new(0, 1), new(1, 1),
            }).SetName("Block"),

            // beehive:
            //   . X X .
            //   X . . X
            //   . X X .
            new TestCaseData((object)new Cell[]
            {
                new(1, 0), new(2, 0),
                new(0, 1), new(3, 1),
                new(1, 2), new(2, 2),
            }).SetName("BeeHive"),

            // loaf:
            //   . X X .
            //   X . . X
            //   . X . X
            //   . . X .
            new TestCaseData((object)new Cell[]
            {
                new(1, 0), new(2, 0),
                new(0, 1), new(3, 1),
                new(1, 2), new(3, 2),
                new(2, 3),
            }).SetName("Loaf"),

            // boat:
            //   X X .
            //   X . X
            //   . X .
            new TestCaseData((object)new Cell[]
            {
                new(0, 0), new(1, 0),
                new(0, 1), new(2, 1),
                new(1, 2),
            }).SetName("Boat"),

            // tub:
            //   . X .
            //   X . X
            //   . X .
            new TestCaseData((object)new Cell[]
            {
                new(1, 0),
                new(0, 1), new(2, 1),
                new(1, 2),
            }).SetName("Tub"),
        ];

        #endregion
    }
}
