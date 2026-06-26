namespace GameOfLife.Api.Service;

/// <summary>
/// Thrown by <see cref="IBoardService.FinalizeBoard"/> when a board fails to settle into a
/// conclusion (a fixed point — still life or extinction) within the caller's iteration budget.
///
/// This is a domain-specific exception rather than a bare <see cref="Exception"/> so the Web
/// layer can catch exactly this case and translate it into the brief's required error response
/// (e.g. HTTP 422) without catching unrelated failures or string-matching a message. It also
/// carries the board id and the budget that was exhausted for logging and diagnostics.
/// </summary>
public class BoardDidNotConcludeException : Exception
{
    public int BoardId { get; }
    public int MaxIterationCount { get; }

    public BoardDidNotConcludeException(int boardId, int maxIterationCount)
        : base($"Board {boardId} did not reach a conclusion within {maxIterationCount} iterations.")
    {
        BoardId = boardId;
        MaxIterationCount = maxIterationCount;
    }
}
