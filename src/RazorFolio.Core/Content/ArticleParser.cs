using System.Collections.ObjectModel;
using System.Globalization;
using Markdig;
using Markdig.Extensions.Yaml;
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

        DateOnly? date = null;
        if (metadata.Date is not null)
        {
            var dateText = metadata.Date.Trim();
            var dateFormat = dateText.Length == 10 ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss";
            // Round-tripping rejects normalization such as 24:00:00 becoming the following day.
            if (!DateTime.TryParseExact(dateText, dateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedDate)
                || parsedDate.ToString(dateFormat, CultureInfo.InvariantCulture) != dateText)
            {
                throw new InvalidDataException(
                    $"Invalid date in '{sourcePath}'. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss with a valid date and time.");
            }

            // Legacy timestamps describe a calendar day; do not convert through the machine's time zone.
            date = DateOnly.FromDateTime(parsedDate);
        }

        var description = string.IsNullOrWhiteSpace(metadata.Description) ? null : metadata.Description.Trim();
        var info = new ArticleInfo(
            title, date, metadata.Draft, description,
            NormalizeNames(metadata.Tags), NormalizeNames(metadata.Category), metadata.Article);
        return new ParsedArticle(
            info, document, ContentUrl.Normalize(metadata.Url, sourcePath), NormalizeAliases(metadata.Aliases, sourcePath));
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
