using Markdig;
using Markdig.Extensions.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RazorFolio.Core.Content;

public sealed class ArticleParser
{
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .Build();

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

        return new ParsedArticle(title, document);
    }
}
