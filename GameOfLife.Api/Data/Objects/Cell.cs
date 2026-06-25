namespace GameOfLife.Api.Data.Objects;

/// <summary>
/// A single live cell's coordinate on the (effectively infinite) board.
/// A readonly record struct because:
///  - the compiler generates correct value equality and a well-distributed GetHashCode,
///    so cells work correctly as members of a HashSet with no hand-written hashing;
///  - being a value type, cells store inline in the set with no per-cell heap allocation,
///    which matters when a board holds large numbers of live cells.
/// </summary>
public readonly record struct Cell(long X, long Y);
