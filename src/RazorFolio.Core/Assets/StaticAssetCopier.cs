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
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Reject file and directory conflicts before copying any assets.
        foreach (var (sourcePath, targetPath) in copies)
        {
            if (reservedPaths.Any(path =>
                string.Equals(path, targetPath, comparison)
                || targetPath.StartsWith(path + Path.DirectorySeparatorChar, comparison)
                || path.StartsWith(targetPath + Path.DirectorySeparatorChar, comparison)))
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
