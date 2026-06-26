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

        #region Oscillators

        // An oscillator returns to its starting state after a fixed period. Stepping one
        // generation lands on the next phase, which (unlike a still life) REQUIRES births:
        // dead cells with exactly 3 live neighbors must come alive. The current Step only
        // counts neighbors of already-live cells, so births never fire — these cases should
        // fail until that gap is closed.
        [TestCaseSource(nameof(OscillatorPatterns))]
        public void Oscillator_AdvancesToNextPhase(Cell[] cells, Cell[] expectedNext)
        {
            var cellsSrc = new HashSet<Cell>(cells);

            HashSet<Cell> actual = _service.Step(cellsSrc);

            Assert.That(actual.SetEquals(new HashSet<Cell>(expectedNext)), Is.True);
        }

        private static readonly TestCaseData[] OscillatorPatterns =
        [
            // blinker (period 2): horizontal bar -> vertical bar.
            //   . . .        . X .
            //   X X X   ->   . X .
            //   . . .        . X .
            new TestCaseData(
                new Cell[] { new(0, 1), new(1, 1), new(2, 1) },
                new Cell[] { new(1, 0), new(1, 1), new(1, 2) })
                .SetName("Blinker"),

            // toad (period 2):
            //   . X X X       . . X .
            //   X X X .  ->   X . . X
            //                 X . . X
            //                 . X . .
            new TestCaseData(
                new Cell[]
                {
                    new(1, 0), new(2, 0), new(3, 0),
                    new(0, 1), new(1, 1), new(2, 1),
                },
                new Cell[]
                {
                    new(2, -1),
                    new(0, 0), new(3, 0),
                    new(0, 1), new(3, 1),
                    new(1, 2),
                })
                .SetName("Toad"),

            // beacon (period 2): the two cells where the blocks touch blink off.
            //   X X . .       X X . .
            //   X X . .       X . . .
            //   . . X X  ->   . . . X
            //   . . X X       . . X X
            new TestCaseData(
                new Cell[]
                {
                    new(0, 0), new(1, 0),
                    new(0, 1), new(1, 1),
                    new(2, 2), new(3, 2),
                    new(2, 3), new(3, 3),
                },
                new Cell[]
                {
                    new(0, 0), new(1, 0),
                    new(0, 1),
                    new(3, 2),
                    new(2, 3), new(3, 3),
                })
                .SetName("Beacon"),
        ];

        #endregion

        #region Spaceships

        // A spaceship returns to its original shape but TRANSLATED after a fixed period,
        // gliding across the board. This is the strongest single check on the stepper: it
        // exercises births, survivals, and deaths together over multiple generations, so any
        // rule error accumulates and corrupts the shape instead of cleanly translating it.
        [TestCaseSource(nameof(SpaceshipPatterns))]
        public void Spaceship_TranslatesAfterOnePeriod(Cell[] cells, int period, long dx, long dy)
        {
            var current = new HashSet<Cell>(cells);

            for (int i = 0; i < period; i++)
            {
                current = _service.Step(current);
            }

            var expected = new HashSet<Cell>(cells.Select(c => new Cell(c.X + dx, c.Y + dy)));

            Assert.That(current.SetEquals(expected), Is.True);
        }

        private static readonly TestCaseData[] SpaceshipPatterns =
        [
            // glider (period 4): travels diagonally, +1 x and +1 y per period.
            //   . X .
            //   . . X
            //   X X X
            new TestCaseData(
                new Cell[] { new(1, 0), new(2, 1), new(0, 2), new(1, 2), new(2, 2) },
                4, 1L, 1L)
                .SetName("Glider"),

            // lightweight spaceship (LWSS, period 4): travels left, -2 x per period.
            //   . X . . X
            //   X . . . .
            //   X . . . X
            //   X X X X .
            new TestCaseData(
                new Cell[]
                {
                    new(1, 0), new(4, 0),
                    new(0, 1),
                    new(0, 2), new(4, 2),
                    new(0, 3), new(1, 3), new(2, 3), new(3, 3),
                },
                4, -2L, 0L)
                .SetName("LightweightSpaceship"),
        ];

        #endregion

        #region Coordinate wraparound (intentional 64-bit overflow behavior)

        // Coordinates are signed 64-bit, and we DELIBERATELY let the stepper's neighbor arithmetic
        // (cell.X +/- 1) overflow/underflow rather than guard or clamp it: long.MaxValue and
        // long.MinValue are treated as adjacent, so the board behaves like a torus at the extreme
        // edges instead of throwing. C#'s default unchecked arithmetic gives us exactly this (the
        // project does not enable CheckForOverflowUnderflow). The README's design log flagged this
        // as something to test on purpose rather than leave as a "big question mark"; this is that
        // test. It also acts as a regression guard — if someone ever switches the build to checked
        // arithmetic, Step would throw at the edge and this test would fail loudly.
        [Test]
        public void Step_BlinkerStraddlingTheIntegerSeam_OscillatesAsIfCoordinatesWrap()
        {
            // A horizontal blinker whose three cells straddle the +X seam: long.MaxValue - 1,
            // long.MaxValue, and then long.MaxValue + 1 which overflows to long.MinValue. Those
            // three are contiguous on the wrapped number line, so this is a normal blinker that
            // happens to sit exactly on the overflow boundary.
            var blinker = new HashSet<Cell>
            {
                new(long.MaxValue - 1, 0),
                new(long.MaxValue, 0),
                new(long.MinValue, 0),     // == long.MaxValue + 1, wrapped
            };

            HashSet<Cell> actual = _service.Step(blinker);

            // A blinker pivots a horizontal bar into a vertical one centered on its middle cell
            // (x == long.MaxValue). Getting this exact result proves the cells at MaxValue and
            // MinValue were counted as neighbors of each other — i.e. the seam wrapped cleanly and
            // did not corrupt the step.
            var expected = new HashSet<Cell>
            {
                new(long.MaxValue, -1),
                new(long.MaxValue, 0),
                new(long.MaxValue, 1),
            };

            Assert.That(actual.SetEquals(expected), Is.True);
        }

        #endregion
    }
}
