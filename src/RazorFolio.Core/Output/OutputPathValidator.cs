namespace RazorFolio.Core.Output;

public static class OutputPathValidator
{
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
