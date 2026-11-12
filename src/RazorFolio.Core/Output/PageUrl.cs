namespace RazorFolio.Core.Output;

public static class PageUrl
{
    public static string GetRelative(string fromPagePath, string toPagePath)
    {
        var directory = Path.GetDirectoryName(fromPagePath)!;
        var segments = Path.GetRelativePath(directory, toPagePath).Split(Path.DirectorySeparatorChar);
        var url = string.Join("/", segments.Select(Uri.EscapeDataString));

        // Keep same-directory links relative, even when a file name contains a colon.
        return segments[0] == ".." ? url : "./" + url;
    }
}
