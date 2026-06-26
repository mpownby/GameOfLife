namespace GameOfLife.Api.Data;

/// <summary>
/// A thin pass-through over the static System.IO calls we use. Its only purpose is to turn
/// un-mockable static methods (File.*, Directory.*) into interface calls so that classes
/// depending on the file system can be unit-tested with a mocked file layer. Implementations
/// must contain NO logic beyond forwarding to the underlying static call.
/// </summary>
public interface IFileIOPassThrough
{
    bool Exists(string path);

    string ReadAllText(string path);

    void WriteAllText(string path, string? contents);

    void Move(string sourceFileName, string destFileName, bool overwrite);

    void CreateDirectory(string path);
}
