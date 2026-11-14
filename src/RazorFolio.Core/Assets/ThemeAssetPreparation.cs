using System.Text;
using RazorFolio.Core.Output;

namespace RazorFolio.Core.Assets;

internal sealed record PreparedThemeAsset(string OutputPath, byte[] Content);

internal static class ThemeAssetPreparation
{
    public static async Task<PreparedThemeAsset[]> PrepareAsync(
        IReadOnlyList<IThemeAsset> assets, string outputDirectory, CancellationToken cancellationToken)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var paths = assets.Select(asset => Path.GetFullPath(asset.RelativePath, outputDirectory)).ToArray();
        foreach (var path in paths)
        {
            var relative = Path.GetRelativePath(outputDirectory, path);
            if (Path.IsPathRooted(relative) || relative.Split('/', '\\').Contains(".."))
            {
                throw new InvalidDataException($"Theme asset must stay inside the output directory: {path}");
            }
        }
        OutputPathValidator.Validate(paths);
        var sources = paths.Select((path, index) => (path, index))
            .ToDictionary(item => item.path, item => item.index, comparer);
        var prepared = new PreparedThemeAsset?[assets.Count];
        var visiting = new HashSet<int>();

        for (var index = 0; index < assets.Count; index++)
        {
            await PrepareAssetAsync(index);
        }
        return prepared.Select(asset => asset!).ToArray();

        async Task<PreparedThemeAsset> PrepareAssetAsync(int index)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (prepared[index] is { } existing) return existing;
            if (!visiting.Add(index)) throw new InvalidDataException($"Cyclic theme asset reference: {paths[index]}");

            var asset = assets[index];
            await using var source = asset.OpenRead();
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, cancellationToken);
            var content = buffer.ToArray();

            if (asset.References.Count > 0)
            {
                var text = Encoding.UTF8.GetString(content);
                // Match longer names first (for example .woff2 before .woff).
                foreach (var reference in asset.References.OrderByDescending(value => value.Length))
                {
                    var dependencyPath = Path.GetFullPath(reference, Path.GetDirectoryName(paths[index])!);
                    if (!sources.TryGetValue(dependencyPath, out var dependencyIndex)
                        || !text.Contains(reference, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException($"Invalid theme asset reference '{reference}' in '{paths[index]}'.");
                    }

                    var dependency = await PrepareAssetAsync(dependencyIndex);
                    var url = PageUrl.GetRelative(paths[index], dependency.OutputPath);
                    text = text.Replace(reference, url, StringComparison.Ordinal);
                }
                content = Encoding.UTF8.GetBytes(text);
            }

            var result = new PreparedThemeAsset(ContentFingerprint.AddToPath(paths[index], content), content);
            prepared[index] = result;
            visiting.Remove(index);
            return result;
        }
    }
}
