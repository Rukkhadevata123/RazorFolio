using System.Collections.ObjectModel;
using System.Globalization;
using Markdig;
using Markdig.Extensions.Yaml;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Serialization;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace RazorFolio.Core.Content;

public sealed class ArticleParser(MarkdownPipeline pipeline)
{
    private readonly IDeserializer deserializer = FrontMatterValueDeserializer.Create();

    public ParsedArticle Parse(string markdown, string sourcePath)
    {
        var document = Markdown.Parse(markdown, pipeline);
        ExplicitHeadingIdsExtension.Validate(document, sourcePath);
        var metadata = new ArticleMetadata();
        using var reader = new StringReader(markdown);

        if (reader.ReadLine()?.TrimEnd() == "---" && document.FirstOrDefault() is not YamlFrontMatterBlock)
        {
            throw new InvalidDataException($"Unclosed front matter in '{sourcePath}'.");
        }

        if (document.FirstOrDefault() is YamlFrontMatterBlock frontMatter)
        {
            try
            {
                // An empty or explicitly null YAML document has no metadata object.
                metadata = deserializer.Deserialize<ArticleMetadata?>(frontMatter.Lines.ToString())
                    ?? new ArticleMetadata();
            }
            catch (YamlException exception)
            {
                throw new InvalidDataException($"Invalid front matter in '{sourcePath}': {exception.Message}", exception);
            }

            // Metadata belongs to the page model, not the rendered article body.
            document.Remove(frontMatter);
        }

        var title = string.IsNullOrWhiteSpace(metadata.Title)
            ? Path.GetFileNameWithoutExtension(sourcePath)
            : metadata.Title.Trim();

        var date = ParseDate(metadata.Date, "date", sourcePath);
        var updated = ParseDate(metadata.Updated, "updated", sourcePath);
        if (updated < date)
        {
            throw new InvalidDataException($"Invalid updated in '{sourcePath}'. It must not be earlier than date.");
        }

        var description = string.IsNullOrWhiteSpace(metadata.Description) ? null : metadata.Description.Trim();
        var info = new ArticleInfo(
            title, date, metadata.Draft, description,
            NormalizeNames(metadata.Tags), NormalizeNames(metadata.Category), metadata.Article)
        {
            Updated = updated,
            ReadingTimeMinutes = ReadingTimeEstimator.EstimateMinutes(document),
            ShareImage = ShareImageConfiguration.Normalize(metadata.ShareImage, sourcePath),
            Star = metadata.Star,
            License = LicenseConfiguration.Normalize(metadata.License, sourcePath)
        };
        return new ParsedArticle(
            info, document, ContentUrl.Normalize(metadata.Url, sourcePath), NormalizeAliases(metadata.Aliases, sourcePath));
    }

    private static DateOnly? ParseDate(string? value, string field, string sourcePath)
    {
        if (value is null) return null;

        if (!DateOnly.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            throw new InvalidDataException($"Invalid {field} in '{sourcePath}'. Use a valid yyyy-MM-dd date.");
        }

        return date;
    }

    private static ReadOnlyCollection<string> NormalizeAliases(List<string?>? aliases, string sourcePath)
    {
        string[] normalized =
        [
            .. (aliases ?? []).Select(alias =>
                    ContentUrl.Normalize(alias, sourcePath, "aliases")
                    ?? throw new InvalidDataException($"Null alias in '{sourcePath}'. Use a site-relative .html path."))
                .Distinct(StringComparer.Ordinal)
        ];
        return Array.AsReadOnly(normalized);
    }

    private static ReadOnlyCollection<string> NormalizeNames(List<string?>? names)
    {
        string[] normalized =
        [
            .. (names ?? [])
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.Trim())
                .Distinct(StringComparer.Ordinal)
        ];
        return Array.AsReadOnly(normalized);
    }
}
