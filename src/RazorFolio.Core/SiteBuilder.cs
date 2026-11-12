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
        var tagsPath = Path.Combine(outputDirectory, "tags.html");
        var hasTags = sources.Any(source => source.Article.Info.Tags.Count > 0);
        string[] reservedPaths = hasTags ? [homePath, tagsPath] : [homePath];
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var reservedPath in reservedPaths)
        {
            if (pagePaths.Any(path => string.Equals(path, reservedPath, comparison)
                || path.StartsWith(reservedPath + Path.DirectorySeparatorChar, comparison)))
            {
                throw new InvalidDataException($"Path reserved for a generated page: {reservedPath}");
            }
        }

        var assetPaths = await assetCopier.CopyAsync(
            Path.Combine(siteRoot, "static"), outputDirectory, [.. pagePaths, .. reservedPaths], cancellationToken);
        var articles = new List<ArticleLink>();

        foreach (var (sourceFile, article) in sources)
        {
            var outputPath = GetOutputPath(sourceFile, contentDirectory, outputDirectory);
            var homeUrl = PageUrl.GetRelative(outputPath, homePath);
            var tagsUrl = PageUrl.GetRelative(outputPath, tagsPath);
            TagLink[] tags =
            [
                .. article.Info.Tags.Select(tag => new TagLink(tag, tagsUrl + "#" + TagGroup.GetId(tag)))
            ];
            var bodyHtml = markdownRenderer.Render(article.Body);
            var page = new ArticlePage(article.Info, bodyHtml, configuration, homeUrl, tags);
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
        var home = new HomePage(orderedArticles, configuration, hasTags ? PageUrl.GetRelative(homePath, tagsPath) : null);
        var homeHtml = await pageRenderer.RenderAsync(home, cancellationToken);
        await File.WriteAllTextAsync(homePath, homeHtml, cancellationToken);

        if (hasTags)
        {
            var archive = new TagArchivePage(
                GroupByTag(orderedArticles), configuration, PageUrl.GetRelative(tagsPath, homePath));
            var tagsHtml = await pageRenderer.RenderAsync(archive, cancellationToken);
            await File.WriteAllTextAsync(tagsPath, tagsHtml, cancellationToken);
        }

        await manifest.CompleteAsync([.. pagePaths, .. reservedPaths, .. assetPaths], cancellationToken);

        return sources.Count + reservedPaths.Length;
    }

    private static TagGroup[] GroupByTag(IReadOnlyList<ArticleLink> articles) =>
    [
        .. articles.SelectMany(article => article.Info.Tags.Select(tag => (Tag: tag, Article: article)))
            .GroupBy(entry => entry.Tag, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new TagGroup(group.Key, [.. group.Select(entry => entry.Article)]))
    ];

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
