namespace RazorFolio.Core.Content;

internal static class ContentUrl
{
    public static string? Normalize(string? value, string sourcePath)
    {
        if (value is null) return null;

        var path = value.Trim();
        var segments = path.TrimStart('/').Split('/');
        if (path.StartsWith("//", StringComparison.Ordinal)
            || !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            || segments.Any(IsInvalidSegment))
        {
            throw new InvalidDataException(
                $"Invalid url in '{sourcePath}'. Use a site-relative .html path without query, fragment, percent encoding or dot segments.");
        }

        return string.Join('/', segments);
    }

    private static bool IsInvalidSegment(string segment)
    {
        if (segment.Length == 0 || segment is "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ')
            || segment.Any(char.IsControl) || segment.IndexOfAny(['\\', ':', '?', '#', '%', '<', '>', '"', '|', '*']) >= 0)
        {
            return true;
        }

        // Keep custom routes portable between Windows development and Linux hosting.
        var stem = segment.Split('.')[0].ToUpperInvariant();
        return stem is "CON" or "PRN" or "AUX" or "NUL"
            || (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT"))
                && "123456789¹²³".Contains(stem[3]));
    }
}
