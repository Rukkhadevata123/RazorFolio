using System.Xml.Linq;

namespace RazorFolio.Core.Output;

public static class SitemapWriter
{
    private const int MaxUrls = 50_000;
    private const int MaxUrlLength = 2_048;
    private const long MaxBytes = 52_428_800;

    public static async Task WriteAsync(
        string outputPath, IReadOnlyList<string> urls, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (urls.Count > MaxUrls || urls.Any(url => url.Length >= MaxUrlLength))
        {
            throw new InvalidDataException("Sitemap supports at most 50,000 URLs, each shorter than 2,048 characters.");
        }

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var document = new XDocument(new XElement(ns + "urlset",
            urls.Select(url => new XElement(ns + "url", new XElement(ns + "loc", url)))));

        // Check the serialized size before replacing an existing sitemap.
        using var buffer = new MemoryStream();
        await document.SaveAsync(buffer, SaveOptions.None, cancellationToken);
        if (buffer.Length > MaxBytes)
        {
            throw new InvalidDataException("Sitemap exceeds 50 MB. Splitting into multiple files is not supported yet.");
        }

        buffer.Position = 0;
        cancellationToken.ThrowIfCancellationRequested();
        await using var target = File.Create(outputPath);
        await buffer.CopyToAsync(target, cancellationToken);
    }
}
