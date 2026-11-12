namespace RazorFolio.Core.Configuration;

internal static class NavigationConfiguration
{
    private static readonly Uri SiteRoot = new("https://razorfolio.invalid/");

    public static NavigationItem[] Normalize(
        IReadOnlyList<NavigationItem>? items, string configurationPath, string field = "navigation")
    {
        if (items is null)
        {
            throw new InvalidDataException($"{field} in '{configurationPath}' must be an array.");
        }

        return items.Select((item, index) => NormalizeItem(item, index, configurationPath, field)).ToArray();
    }

    private static NavigationItem NormalizeItem(NavigationItem? item, int index, string configurationPath, string field)
    {
        var location = $"{field}[{index}] in '{configurationPath}'";
        if (item is null || string.IsNullOrWhiteSpace(item.Text) || string.IsNullOrWhiteSpace(item.Link))
        {
            throw new InvalidDataException($"{location} must have non-empty text and link values.");
        }

        var link = item.Link.Trim();
        if (link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || link.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out var external)
                || !external.IsWellFormedOriginalString() || external.UserInfo.Length > 0)
            {
                throw new InvalidDataException($"Invalid HTTP(S) link in {location}.");
            }

            return new NavigationItem(item.Text.Trim(), external.AbsoluteUri);
        }

        var suffixStart = link.IndexOfAny(['?', '#']);
        var path = suffixStart < 0 ? link : link[..suffixStart];
        var suffix = suffixStart < 0 ? "" : link[suffixStart..];
        var segments = path.Split('/').Select(Uri.UnescapeDataString);
        if (link.StartsWith("//", StringComparison.Ordinal) || link.Contains('\\') || link.Any(char.IsControl)
            || segments.Any(segment => segment is "." or ".." || segment.IndexOfAny([':', '/', '\\']) >= 0))
        {
            throw new InvalidDataException($"Invalid link in {location}. Use a site-relative path or an HTTP(S) URL.");
        }

        path = path.TrimStart('/');
        if (path.Length == 0)
        {
            path = "index.html";
        }

        if (!Uri.TryCreate(SiteRoot, path + suffix, out var target))
        {
            throw new InvalidDataException($"Invalid site-relative link in {location}.");
        }

        return new NavigationItem(item.Text.Trim(), target.PathAndQuery.TrimStart('/') + target.Fragment);
    }
}
