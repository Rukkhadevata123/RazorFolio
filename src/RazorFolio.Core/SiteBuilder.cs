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
        var homePath = Path.Combine(outputDirectory, "index.html");
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (pagePaths.Any(path => string.Equals(path, homePath, comparison)
            || path.StartsWith(homePath + Path.DirectorySeparatorChar, comparison)))
        {
            throw new InvalidDataException("The root index.html path is reserved for the generated home page.");
        }

        await assetCopier.CopyAsync(
            Path.Combine(siteRoot, "static"), outputDirectory, [.. pagePaths, homePath], cancellationToken);
        var articles = new List<ArticleLink>();

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
            articles.Add(new ArticleLink(article.Title, GetPageUrl(outputPath, outputDirectory)));
        }

        var homeHtml = await pageRenderer.RenderAsync(new HomePage(articles), cancellationToken);
        await File.WriteAllTextAsync(homePath, homeHtml, cancellationToken);

        return sourceFiles.Length + 1;
    }

    private static string GetOutputPath(string sourceFile, string contentDirectory, string outputDirectory)
    {
        var relativePath = Path.GetRelativePath(contentDirectory, sourceFile);
        return Path.Combine(outputDirectory, Path.ChangeExtension(relativePath, ".html"));
    }

    private static string GetPageUrl(string outputPath, string outputDirectory)
    {
        var segments = Path.GetRelativePath(outputDirectory, outputPath).Split(Path.DirectorySeparatorChar);
        return "./" + string.Join("/", segments.Select(Uri.EscapeDataString));
    }
}
