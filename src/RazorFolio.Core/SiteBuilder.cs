using RazorFolio.Core.Assets;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Content;
using RazorFolio.Core.Output;
using RazorFolio.Core.Rendering;

namespace RazorFolio.Core;

public sealed class SiteBuilder(
    ArticleParser articleParser,
    MarkdownRenderer markdownRenderer,
    IPageRenderer pageRenderer,
    StaticAssetCopier assetCopier)
{
    public async Task<int> BuildAsync(
        SiteBuildTarget target, CancellationToken cancellationToken = default)
    {
        var siteRoot = target.SiteDirectory;
        var configuration = await SiteConfigurationLoader.LoadAsync(siteRoot, cancellationToken);
        var manifest = await OutputManifest.LoadAsync(target.OutputDirectory, target.ManifestPath, cancellationToken);
        var contentDirectory = Path.Combine(siteRoot, "content");
        var outputDirectory = target.OutputDirectory;

        if (!Directory.Exists(contentDirectory))
        {
            throw new DirectoryNotFoundException($"Content directory not found: {contentDirectory}");
        }

        string[] sourceFiles =
        [
            .. Directory.EnumerateFiles(contentDirectory, "*", SearchOption.AllDirectories)
                .Where(path => Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase))
        ];
        Array.Sort(sourceFiles, StringComparer.Ordinal);
        var sources = await LoadArticlesAsync(sourceFiles, target.IncludeDrafts, cancellationToken);
        Directory.CreateDirectory(outputDirectory);

        string[] pagePaths =
        [
            .. sources.Select(source => GetOutputPath(source.Path, contentDirectory, outputDirectory))
        ];
        var homePath = Path.Combine(outputDirectory, "index.html");
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (pagePaths.Any(path => string.Equals(path, homePath, comparison)
            || path.StartsWith(homePath + Path.DirectorySeparatorChar, comparison)))
        {
            throw new InvalidDataException("The root index.html path is reserved for the generated home page.");
        }

        var assetPaths = await assetCopier.CopyAsync(
            Path.Combine(siteRoot, "static"), outputDirectory, [.. pagePaths, homePath], cancellationToken);
        var articles = new List<ArticleLink>();

        foreach (var (sourceFile, article) in sources)
        {
            var outputPath = GetOutputPath(sourceFile, contentDirectory, outputDirectory);
            var homeUrl = PageUrl.GetRelative(outputPath, homePath);
            var bodyHtml = markdownRenderer.Render(article.Body);
            var page = new ArticlePage(article.Info, bodyHtml, configuration, homeUrl);
            var html = await pageRenderer.RenderAsync(page, cancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
            var articleUrl = PageUrl.GetRelative(homePath, outputPath);
            articles.Add(new ArticleLink(article.Info, articleUrl));
        }

        ArticleLink[] orderedArticles =
        [
            .. articles.OrderByDescending(article => article.Info.Date)
                .ThenBy(article => article.Url, StringComparer.Ordinal)
        ];
        var homeHtml = await pageRenderer.RenderAsync(new HomePage(orderedArticles, configuration), cancellationToken);
        await File.WriteAllTextAsync(homePath, homeHtml, cancellationToken);
        await manifest.CompleteAsync([.. pagePaths, homePath, .. assetPaths], cancellationToken);

        return sources.Count + 1;
    }

    private async Task<List<(string Path, ParsedArticle Article)>> LoadArticlesAsync(
        IEnumerable<string> sourceFiles, bool includeDrafts, CancellationToken cancellationToken)
    {
        var sources = new List<(string Path, ParsedArticle Article)>();
        foreach (var path in sourceFiles)
        {
            var markdown = await File.ReadAllTextAsync(path, cancellationToken);
            var article = articleParser.Parse(markdown, path);
            if (includeDrafts || !article.Info.Draft)
            {
                sources.Add((path, article));
            }
        }

        return sources;
    }

    private static string GetOutputPath(string sourceFile, string contentDirectory, string outputDirectory)
    {
        var relativePath = Path.GetRelativePath(contentDirectory, sourceFile);
        return Path.Combine(outputDirectory, Path.ChangeExtension(relativePath, ".html"));
    }
}
