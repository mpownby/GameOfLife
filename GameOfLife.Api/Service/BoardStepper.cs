using GameOfLife.Api.Data.Objects;

namespace GameOfLife.Api.Service;

/// <summary>
/// Implementation of <see cref="IBoardStepper"/>: the concrete Game of Life stepping rule.
/// </summary>
public class BoardStepper : IBoardStepper
{
    public HashSet<Cell> Step(IReadOnlySet<Cell> liveCells)
    {
        throw new NotImplementedException();
    }
}
