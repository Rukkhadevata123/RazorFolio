using System.Collections.ObjectModel;
using System.Globalization;
using Markdig;
using Markdig.Extensions.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RazorFolio.Core.Content;

public sealed class ArticleParser(MarkdownPipeline pipeline)
{
    private readonly IDeserializer deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .WithDuplicateKeyChecking()
        .Build();

    public ParsedArticle Parse(string markdown, string sourcePath)
    {
        var document = Markdown.Parse(markdown, pipeline);
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
                // YAML null documents can deserialize to null despite the return annotation.
                metadata = deserializer.Deserialize<ArticleMetadata>(frontMatter.Lines.ToString())
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
            if (!DateOnly.TryParseExact(metadata.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate))
            {
                throw new InvalidDataException($"Invalid date in '{sourcePath}'. Use a valid yyyy-MM-dd date.");
            }

            date = parsedDate;
        }

        var description = string.IsNullOrWhiteSpace(metadata.Description) ? null : metadata.Description.Trim();
        var info = new ArticleInfo(
            title, date, metadata.Draft, description,
            NormalizeNames(metadata.Tags), NormalizeNames(metadata.Category), metadata.Article);
        return new ParsedArticle(info, document, ContentUrl.Normalize(metadata.Url, sourcePath));
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
