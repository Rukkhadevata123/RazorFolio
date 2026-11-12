using System.Text.Json;

namespace RazorFolio.Core.Output;

public sealed class OutputManifest
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

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
        string[] files = [];
        if (File.Exists(manifestPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                files = JsonSerializer.Deserialize<string[]>(json)
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

        // Do not follow output symlinks when deleting files recorded by an earlier build.
        foreach (var path in stalePaths)
        {
            for (var entry = path; entry is not null; entry = Path.GetDirectoryName(entry))
            {
                if ((File.Exists(entry) || Directory.Exists(entry))
                    && (File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException($"Refusing to clean output through a symbolic link: {entry}");
                }

                if (comparer.Equals(entry, outputDirectory))
                {
                    break;
                }
            }
        }

        foreach (var path in stalePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        var json = JsonSerializer.Serialize(currentFiles.Order(comparer), Options);
        var temporaryPath = manifestPath + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
        File.Move(temporaryPath, manifestPath, overwrite: true);
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
