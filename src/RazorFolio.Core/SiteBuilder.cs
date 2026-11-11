using RazorFolio.Core.Content;
using RazorFolio.Core.Rendering;

namespace RazorFolio.Core;

public sealed class SiteBuilder(
    ArticleParser articleParser,
    MarkdownRenderer markdownRenderer,
    IPageRenderer pageRenderer)
{
    public async Task<int> BuildAsync(string siteDirectory, CancellationToken cancellationToken = default)
    {
        var siteRoot = Path.GetFullPath(siteDirectory);
        var contentDirectory = Path.Combine(siteRoot, "content");
        var outputDirectory = Path.Combine(siteRoot, "public");

        if (!Directory.Exists(contentDirectory))
        {
            throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");
        }

        var sourceFiles = Directory.GetFiles(contentDirectory, "*.md", SearchOption.TopDirectoryOnly);
        Array.Sort(sourceFiles, StringComparer.Ordinal);
        Directory.CreateDirectory(outputDirectory);

        foreach (var sourceFile in sourceFiles)
        {
            var markdown = await File.ReadAllTextAsync(sourceFile, cancellationToken);
            var article = articleParser.Parse(markdown, sourceFile);
            var bodyHtml = markdownRenderer.Render(article.Body);
            var page = new ArticlePage(article.Title, bodyHtml);
            var html = await pageRenderer.RenderAsync(page, cancellationToken);
            var outputName = Path.ChangeExtension(Path.GetFileName(sourceFile), ".html");
            var outputPath = Path.Combine(outputDirectory, outputName);

            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
        }

        return sourceFiles.Length;
    }
}
