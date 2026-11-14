using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using RazorFolio.Core.Serialization;

namespace RazorFolio.Core.Output;

public static class SearchIndexWriter
{
    private static readonly RazorFolioJsonContext Json = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    });

    public static async Task WriteAsync(
        string outputPath,
        IReadOnlyList<SearchDocument> documents,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = new SearchIndex(1, [.. documents.OrderBy(document => document.Url, StringComparer.Ordinal)]);
        await using var target = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(target, index, Json.SearchIndex, cancellationToken);
    }

    internal sealed record SearchIndex(int Version, IReadOnlyList<SearchDocument> Documents);
}
