using RazorFolio.Core.Assets;
using RazorFolio.Core.Content;
using RazorFolio.Core.Rendering;

namespace RazorFolio.Core;

public sealed class SiteBuilder(
    ArticleParser articleParser,
    MarkdownRenderer markdownRenderer,
    IPageRenderer pageRenderer,
    StaticAssetCopier assetCopier)
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

        var sourceFiles = Directory.GetFiles(contentDirectory, "*.md", SearchOption.AllDirectories);
        Array.Sort(sourceFiles, StringComparer.Ordinal);
        Directory.CreateDirectory(outputDirectory);

        var pagePaths = sourceFiles.Select(source =>
            GetOutputPath(source, contentDirectory, outputDirectory)).ToArray();
        await assetCopier.CopyAsync(Path.Combine(siteRoot, "static"), outputDirectory, pagePaths, cancellationToken);

        foreach (var sourceFile in sourceFiles)
        {
            var markdown = await File.ReadAllTextAsync(sourceFile, cancellationToken);
            var article = articleParser.Parse(markdown, sourceFile);
            var bodyHtml = markdownRenderer.Render(article.Body);
            var page = new ArticlePage(article.Title, bodyHtml);
            var html = await pageRenderer.RenderAsync(page, cancellationToken);
            var outputPath = GetOutputPath(sourceFile, contentDirectory, outputDirectory);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
        }

        return sourceFiles.Length;
    }

    private static string GetOutputPath(string sourceFile, string contentDirectory, string outputDirectory)
    {
        var relativePath = Path.GetRelativePath(contentDirectory, sourceFile);
        return Path.Combine(outputDirectory, Path.ChangeExtension(relativePath, ".html"));
    }
}
