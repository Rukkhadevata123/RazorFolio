namespace RazorFolio.Core.Output;

public sealed class ContentLinkResolver(Dictionary<string, string> contentPaths)
{
    public string Resolve(string link, string originalPagePath, string outputPagePath)
    {
        if (link.Length == 0 || link[0] is '/' or '#' or '?' || link.Contains('\\')
            || Uri.TryCreate(link, UriKind.Absolute, out _))
        {
            return link;
        }

        var suffixStart = link.IndexOfAny(['?', '#']);
        var path = Uri.UnescapeDataString(suffixStart < 0 ? link : link[..suffixStart]);
        var suffix = suffixStart < 0 ? "" : link[suffixStart..];
        if (Path.IsPathRooted(path)) return link;

        string targetPath;
        try
        {
            targetPath = Path.GetFullPath(path, Path.GetDirectoryName(originalPagePath)!);
        }
        catch (ArgumentException)
        {
            // Leave malformed author URLs to the browser; this is not a link checker.
            return link;
        }

        if (Path.GetExtension(targetPath).Equals(".md", StringComparison.OrdinalIgnoreCase))
        {
            targetPath = contentPaths.GetValueOrDefault(targetPath) ?? Path.ChangeExtension(targetPath, ".html");
        }

        var relativeUrl = PageUrl.GetRelative(outputPagePath, targetPath);
        if (path.EndsWith('/') && !relativeUrl.EndsWith('/')) relativeUrl += "/";
        return relativeUrl + suffix;
    }
}
