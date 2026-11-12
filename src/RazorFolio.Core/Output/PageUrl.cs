namespace RazorFolio.Core.Output;

public static class PageUrl
{
    public static string ResolveNavigation(string link, string fromPagePath, string homePath)
    {
        if (Uri.TryCreate(link, UriKind.Absolute, out var external) && external.Scheme is "http" or "https")
        {
            return link;
        }

        // Normalized internal links are rooted at the site, regardless of its deployment path.
        var homeUrl = GetRelative(fromPagePath, homePath);
        return homeUrl[..^Path.GetFileName(homePath).Length] + link;
    }

    public static string? GetAbsolute(string pagePath, string outputDirectory, string? siteUrl)
    {
        if (siteUrl is null)
        {
            return null;
        }

        var relativeUrl = GetRelative(Path.Combine(outputDirectory, "index.html"), pagePath);
        return GetAbsolute(relativeUrl, siteUrl);
    }

    public static string? GetAbsolute(string relativeUrl, string? siteUrl)
    {
        if (siteUrl is null)
        {
            return null;
        }

        if (relativeUrl == "./index.html")
        {
            relativeUrl = "./";
        }

        return new Uri(new Uri(siteUrl), relativeUrl).AbsoluteUri;
    }

    public static string GetRelative(string fromPagePath, string toPagePath)
    {
        var directory = Path.GetDirectoryName(fromPagePath)!;
        var segments = Path.GetRelativePath(directory, toPagePath).Split(Path.DirectorySeparatorChar);
        var url = string.Join("/", segments.Select(Uri.EscapeDataString));

        // Keep same-directory links relative, even when a file name contains a colon.
        return segments[0] == ".." ? url : "./" + url;
    }
}
