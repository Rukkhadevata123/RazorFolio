namespace RazorFolio.Core.Output;

public static class OutputPathValidator
{
    internal static bool PathsConflict(string first, string second)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(first, second, comparison)
            || first.StartsWith(second + Path.DirectorySeparatorChar, comparison)
            || second.StartsWith(first + Path.DirectorySeparatorChar, comparison);
    }

    internal static void ValidateReservedPaths(IReadOnlyList<string> contentPaths, IReadOnlyList<string> reservedPaths)
    {
        foreach (var reservedPath in reservedPaths)
        {
            if (contentPaths.Any(path => PathsConflict(path, reservedPath)))
            {
                throw new InvalidDataException($"Path reserved for generated output: {reservedPath}");
            }
        }
    }

    public static void Validate(IReadOnlyCollection<string> outputPaths)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var files = new HashSet<string>(comparer);
        foreach (var path in outputPaths)
        {
            if (!files.Add(path))
            {
                throw new InvalidDataException($"Multiple generated files use the same output path: {path}");
            }
        }

        foreach (var path in outputPaths)
        {
            for (var directory = Path.GetDirectoryName(path);
                 directory is not null;
                 directory = Path.GetDirectoryName(directory))
            {
                if (files.Contains(directory))
                {
                    throw new InvalidDataException($"Output path is required as both a file and a directory: {directory}");
                }
            }
        }
    }
}
