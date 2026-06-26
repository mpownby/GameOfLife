namespace GameOfLife.Api.Data;

/// <summary>
/// Low-level persistence of the serialized board catalog. Abstracts the actual storage
/// medium (a file on disk) away from the repository, so the repository's logic
/// (indexing, locking, copying, serialization) can be unit-tested without real I/O.
/// </summary>
public interface IBoardStateStore
{
    /// <summary>Reads the persisted contents, or null if nothing has been persisted yet.</summary>
    string? Load();

    /// <summary>Persists the given contents, replacing whatever was there before.</summary>
    void Save(string contents);
}
