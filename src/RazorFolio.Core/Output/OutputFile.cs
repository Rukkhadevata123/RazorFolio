using System.Text;

namespace RazorFolio.Core.Output;

internal static class OutputFile
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static void CreateDirectory(string path)
    {
        RequireNoLinks(path);
        Directory.CreateDirectory(path);
    }

    public static Task WriteTextAsync(string path, string content, CancellationToken cancellationToken) =>
        WriteAsync(path, async (stream, token) =>
        {
            await using var writer = new StreamWriter(stream, Utf8, leaveOpen: true);
            await writer.WriteAsync(content.AsMemory(), token);
        }, cancellationToken);

    public static Task WriteBytesAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
        WriteAsync(path, (stream, token) => stream.WriteAsync(content, token).AsTask(), cancellationToken);

    public static async Task WriteAsync(
        string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        path = Path.GetFullPath(path);
        RequireNoLinks(path);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".rf-{Guid.NewGuid():N}.tmp");
        var ownsTemporaryFile = false;
        try
        {
            await using (var stream = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                ownsTemporaryFile = true;
                await write(stream, cancellationToken);
            }

            // Recheck the destination after the asynchronous write and honor cancellation before installing it.
            RequireNoLinks(path);
            RequireOrdinaryEntry(temporaryPath);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception failure)
        {
            if (ownsTemporaryFile)
            {
                try
                {
                    RequireNoLinks(temporaryPath);
                    File.Delete(temporaryPath);
                }
                catch (Exception cleanupFailure) when (cleanupFailure is IOException or UnauthorizedAccessException or InvalidDataException)
                {
                    throw new IOException($"Could not clean up temporary output: {temporaryPath}",
                        new AggregateException(failure, cleanupFailure));
                }
            }

            throw;
        }
    }

    public static void RequireNoLinks(string path)
    {
        CheckPath(Path.GetFullPath(path));

        static void CheckPath(string entry)
        {
            if (Path.GetDirectoryName(entry) is { } parent)
            {
                CheckPath(parent);
            }
            RequireOrdinaryEntry(entry);
        }
    }

    private static void RequireOrdinaryEntry(string path)
    {
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(path);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        // Exists checks would skip dangling links.
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Refusing to use a symbolic link or junction in an output path: {path}");
        }
    }
}
