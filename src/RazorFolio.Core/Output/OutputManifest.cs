using System.Text.Json;
using RazorFolio.Core.Serialization;

namespace RazorFolio.Core.Output;

public sealed class OutputManifest
{
    private static readonly RazorFolioJsonContext Json = new(new JsonSerializerOptions { WriteIndented = true });

    private readonly string outputDirectory;
    private readonly string manifestPath;
    private readonly string[] previousFiles;

    private OutputManifest(string outputDirectory, string manifestPath, string[] previousFiles)
    {
        this.outputDirectory = outputDirectory;
        this.manifestPath = manifestPath;
        this.previousFiles = previousFiles;
    }

    public static async Task<OutputManifest> LoadAsync(
        string outputDirectory, string manifestPath, CancellationToken cancellationToken)
    {
        OutputFile.RequireNoLinks(manifestPath);
        string[] files = [];
        if (File.Exists(manifestPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                files = JsonSerializer.Deserialize(json, RazorFolioJsonContext.Default.StringArray)
                    ?? throw new InvalidDataException($"Output manifest is null: {manifestPath}");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"Invalid output manifest: {manifestPath}", exception);
            }
        }

        var manifest = new OutputManifest(outputDirectory, manifestPath, files);
        foreach (var file in files)
        {
            manifest.GetSafePath(file);
        }

        return manifest;
    }

    public async Task CompleteAsync(IEnumerable<string> outputPaths, CancellationToken cancellationToken)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var currentFiles = outputPaths.Select(path => Path.GetRelativePath(outputDirectory, path))
            .ToHashSet(comparer);
        string[] stalePaths = [.. previousFiles.Except(currentFiles, comparer).Select(GetSafePath)];

        OutputFile.RequireNoLinks(manifestPath);
        // Validate all stale paths before deleting any of them.
        foreach (var path in stalePaths)
        {
            OutputFile.RequireNoLinks(path);
        }

        foreach (var path in stalePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OutputFile.RequireNoLinks(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        var orderedFiles = currentFiles.Order(comparer).ToArray();
        var json = JsonSerializer.Serialize(orderedFiles, Json.StringArray);
        await OutputFile.WriteTextAsync(manifestPath, json, cancellationToken);
    }

    private string GetSafePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath)
            || relativePath.Split('/', '\\').Any(segment => segment is ".." or "."))
        {
            throw new InvalidDataException($"Invalid path in output manifest: {relativePath}");
        }

        return Path.Combine(outputDirectory, relativePath);
    }
}
