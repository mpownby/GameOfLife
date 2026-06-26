namespace GameOfLife.Api.Data;

/// <summary>
/// Production implementation of <see cref="IFileIOPassThrough"/>. Each method does nothing but
/// forward to the corresponding static System.IO call — no logic lives here, which is why this
/// class itself does not need (and cannot meaningfully have) unit tests.
/// </summary>
public class FileIOPassThrough : IFileIOPassThrough
{
    public bool Exists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string? contents) => File.WriteAllText(path, contents);

    public void Move(string sourceFileName, string destFileName, bool overwrite) =>
        File.Move(sourceFileName, destFileName, overwrite);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
}
