namespace RazorFolio.Core.Configuration;

internal static class ShareImageConfiguration
{
    public static string? Normalize(string? value, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var image = value.Trim();
        if (image.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || image.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(image, UriKind.Absolute, out var url)
                && url.IsWellFormedOriginalString() && url.UserInfo.Length == 0 && url.Fragment.Length == 0)
            {
                return url.AbsoluteUri;
            }
        }
        else
        {
            var path = image.Split('?')[0];
            var segments = path.Split('/').Select(Uri.UnescapeDataString);
            if (path.Trim('/').Length > 0 && !image.StartsWith("//", StringComparison.Ordinal)
                && !image.Contains('\\') && !image.Contains('#') && !image.Any(char.IsControl)
                && !segments.Any(segment => segment is "." or ".." || segment.IndexOfAny([':', '/', '\\']) >= 0)
                && Uri.TryCreate(new Uri("https://razorfolio.invalid/"), image.TrimStart('/'), out var relative))
            {
                return relative.PathAndQuery.TrimStart('/');
            }
        }

        throw new InvalidDataException(
            $"Invalid shareImage in '{sourcePath}'. Use a site-relative image path or an HTTP(S) URL without credentials or fragment.");
    }
}
