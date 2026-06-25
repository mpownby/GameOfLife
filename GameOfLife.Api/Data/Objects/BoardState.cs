namespace GameOfLife.Api.Data.Objects;

public class BoardState
{
    /// <summary>
    /// The board exactly as it was uploaded.
    /// </summary>
    public HashSet<Cell> InitialState { get; set; } = [];

    /// <summary>
    /// The live cells after <see cref="IterationCount"/> iterations have run.
    /// </summary>
    public HashSet<Cell> CurrentState { get; set; } = [];

    /// <summary>
    /// Number of iterations that have run to get from the initial state to the current state.
    /// </summary>
    public int IterationCount { get; set; }

    // We store both the initial and current state even though current could be re-derived by
    // replaying iterations from initial. Replaying every time has a performance cost, disk is
    // cheap, and keeping the current state lets us diagnose (or confirm) the algorithm's behavior
    // in a way we otherwise couldn't. It's a defensive measure that costs very little.
}
