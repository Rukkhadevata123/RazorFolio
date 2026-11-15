using RazorFolio.Core.Output;

namespace RazorFolio.Core.Assets;

public sealed class StaticAssetCopier
{
    public async Task<IReadOnlyList<string>> CopyAsync(
        string sourceDirectory,
        string outputDirectory,
        IReadOnlyCollection<string> reservedPaths,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return [];
        }

        var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
        (string Source, string Target)[] copies =
        [
            .. files.Select(source => (
                source,
                Path.Combine(outputDirectory, Path.GetRelativePath(sourceDirectory, source))))
        ];
        // Reject file and directory conflicts before copying any assets.
        foreach (var (sourcePath, targetPath) in copies)
        {
            if (reservedPaths.Any(path => OutputPathValidator.PathsConflict(path, targetPath)))
            {
                throw new InvalidDataException($"Static asset conflicts with generated output: {sourcePath}");
            }
        }

        foreach (var (sourcePath, targetPath) in copies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            await using var source = File.OpenRead(sourcePath);
            await using var target = File.Create(targetPath);
            await source.CopyToAsync(target, cancellationToken);
        }

        return [.. copies.Select(copy => copy.Target)];
    }
}
