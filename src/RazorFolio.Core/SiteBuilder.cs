using System.Globalization;
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
    StaticAssetCopier assetCopier,
    IThemeAsset stylesheet)
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
        var allSources = await LoadArticlesAsync(sourceFiles, cancellationToken);
        var sources = allSources.Where(source => target.IncludeDrafts || !source.Article.Info.Draft).ToList();

        var homePath = Path.Combine(outputDirectory, "index.html");
        var articleCount = sources.Count(source => source.Article.Info.IsArticle);
        var homePageCount = articleCount == 0 ? 1 : 1 + (articleCount - 1) / configuration.PageSize;
        string[] homePaths =
        [
            .. Enumerable.Range(1, homePageCount).Select(number => number == 1
                ? homePath
                : Path.Combine(outputDirectory, "page", number.ToString(CultureInfo.InvariantCulture) + ".html"))
        ];
        ArticleOutput[] allOutputs =
        [
            .. allSources.Select(source =>
            {
                var originalPath = Path.Combine(outputDirectory, Path.GetRelativePath(contentDirectory, source.Path));
                var outputPath = source.Article.OutputUrl is { } customUrl
                    ? Path.Combine(outputDirectory, customUrl.Replace('/', Path.DirectorySeparatorChar))
                    : Path.ChangeExtension(originalPath, ".html");
                return new ArticleOutput(source.Article, outputPath, PageUrl.GetRelative(homePath, outputPath), originalPath);
            })
        ];
        ArticleOutput[] outputs = [.. allOutputs.Where(output => target.IncludeDrafts || !output.Article.Info.Draft)];
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var linkResolver = new ContentLinkResolver(allOutputs.ToDictionary(
            output => output.OriginalPath, output => output.OutputPath, pathComparer));
        string[] pagePaths = [.. outputs.Select(output => output.OutputPath)];
        var aliases = outputs.SelectMany(output => output.Article.Aliases.Select(alias => new AliasOutput(
            Path.Combine(outputDirectory, alias.Replace('/', Path.DirectorySeparatorChar)), output.OutputPath))).ToArray();
        string[] aliasPaths = [.. aliases.Select(alias => alias.OutputPath)];
        string[] contentPaths = [.. pagePaths, .. aliasPaths];
        OutputPathValidator.Validate(contentPaths);
        var tagsPath = Path.Combine(outputDirectory, "tags.html");
        var hasTags = sources.Any(source => source.Article.Info.IsArticle && source.Article.Info.Tags.Count > 0);
        var tagPaths = sources.Where(source => source.Article.Info.IsArticle)
            .SelectMany(source => source.Article.Info.Tags)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(tag => tag, tag => TagPagePath.GetOutputPath(outputDirectory, tag), StringComparer.Ordinal);
        var categoriesPath = Path.Combine(outputDirectory, "categories.html");
        var hasCategories = sources.Any(source => source.Article.Info.IsArticle && source.Article.Info.Categories.Count > 0);
        var archivesPath = Path.Combine(outputDirectory, "archives.html");
        var hasArticles = articleCount > 0;
        List<string> reservedPaths = [.. homePaths];
        if (hasTags)
        {
            reservedPaths.Add(tagsPath);
            reservedPaths.AddRange(tagPaths.Values);
        }
        if (hasCategories)
        {
            reservedPaths.Add(categoriesPath);
        }
        if (hasArticles)
        {
            reservedPaths.Add(archivesPath);
        }

        var generateMetadata = configuration.Url is not null && !target.IncludeDrafts;
        var sitemapPath = Path.Combine(outputDirectory, "sitemap.xml");
        var feedPath = Path.Combine(outputDirectory, "feed.xml");
        var feedUrl = generateMetadata ? PageUrl.GetAbsolute(feedPath, outputDirectory, configuration.Url) : null;
        string[] metadataPaths = generateMetadata ? [sitemapPath, feedPath] : [];
        var stylesheetPath = Path.GetFullPath(stylesheet.RelativePath, outputDirectory);
        string[] protectedPaths = [.. reservedPaths, .. metadataPaths, stylesheetPath];
        OutputPathValidator.Validate(protectedPaths);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (var protectedPath in protectedPaths)
        {
            if (contentPaths.Any(path => string.Equals(path, protectedPath, comparison)
                || path.StartsWith(protectedPath + Path.DirectorySeparatorChar, comparison)
                || protectedPath.StartsWith(path + Path.DirectorySeparatorChar, comparison)))
            {
                throw new InvalidDataException($"Path reserved for generated output: {protectedPath}");
            }
        }

        Directory.CreateDirectory(outputDirectory);
        var assetPaths = await assetCopier.CopyAsync(
            Path.Combine(siteRoot, "static"), outputDirectory, [.. contentPaths, .. protectedPaths], cancellationToken);
        await CopyStylesheetAsync(stylesheetPath, cancellationToken);

        var customStylesheetPath = FindCopiedAsset("custom.css");
        var faviconPath = FindCopiedAsset("favicon.svg");
        string[] stylesheetPaths = customStylesheetPath is null
            ? [stylesheetPath]
            : [stylesheetPath, customStylesheetPath];

        ArticleOutput[] orderedArticlePages =
        [
            .. outputs.Where(output => output.Article.Info.IsArticle)
                .OrderByDescending(output => output.Article.Info.Date)
                .ThenBy(output => output.Url, StringComparer.Ordinal)
        ];
        var navigations = orderedArticlePages.Select((output, index) => KeyValuePair.Create(
            output.OutputPath,
            new ArticleNavigation(
                GetAdjacentArticle(orderedArticlePages, index - 1, output.OutputPath),
                GetAdjacentArticle(orderedArticlePages, index + 1, output.OutputPath))))
            .ToDictionary();

        foreach (var output in outputs)
        {
            var article = output.Article;
            var outputPath = output.OutputPath;
            var homeUrl = PageUrl.GetRelative(outputPath, homePath);
            TagLink[] tags = article.Info.IsArticle ?
            [
                .. article.Info.Tags.Select(tag => new TagLink(tag, PageUrl.GetRelative(outputPath, tagPaths[tag])))
            ] : [];
            var categoriesUrl = PageUrl.GetRelative(outputPath, categoriesPath);
            CategoryLink[] categories = article.Info.IsArticle ?
            [
                .. article.Info.Categories.Select(category =>
                    new CategoryLink(category, categoriesUrl + "#" + CategoryGroup.GetId(category)))
            ] : [];
            var navigation = navigations.GetValueOrDefault(outputPath) ?? new ArticleNavigation(null, null);
            var bodyHtml = markdownRenderer.Render(article.Body,
                link => linkResolver.Resolve(link, output.OriginalPath, outputPath));
            var page = new ArticlePage(
                article.Info, bodyHtml, CreateContext(outputPath, article.Info.Draft), homeUrl, tags, categories, navigation);
            var html = await pageRenderer.RenderAsync(page, cancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
        }

        ArticleLink[] orderedArticles =
        [
            .. orderedArticlePages.Select(output => new ArticleLink(output.Article.Info, output.Url))
        ];
        for (var index = 0; index < homePaths.Length; index++)
        {
            var outputPath = homePaths[index];
            ArticleLink[] pageArticles =
            [
                .. orderedArticlePages.Skip(index * configuration.PageSize).Take(configuration.PageSize)
                    .Select(output => new ArticleLink(output.Article.Info, PageUrl.GetRelative(outputPath, output.OutputPath)))
            ];
            var pagination = new Pagination(
                index + 1,
                homePaths.Length,
                index > 0 ? PageUrl.GetRelative(outputPath, homePaths[index - 1]) : null,
                index + 1 < homePaths.Length ? PageUrl.GetRelative(outputPath, homePaths[index + 1]) : null);
            var home = new HomePage(
                pageArticles, CreateContext(outputPath),
                hasTags ? PageUrl.GetRelative(outputPath, tagsPath) : null,
                hasCategories ? PageUrl.GetRelative(outputPath, categoriesPath) : null,
                hasArticles ? PageUrl.GetRelative(outputPath, archivesPath) : null, pagination);
            var homeHtml = await pageRenderer.RenderAsync(home, cancellationToken);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, homeHtml, cancellationToken);
        }

        if (hasTags)
        {
            var tagGroups = GroupByTag(orderedArticles, tagsPath, tagPaths);
            var archive = new TagArchivePage(
                tagGroups, CreateContext(tagsPath), PageUrl.GetRelative(tagsPath, homePath));
            var tagsHtml = await pageRenderer.RenderAsync(archive, cancellationToken);
            await File.WriteAllTextAsync(tagsPath, tagsHtml, cancellationToken);

            var articlePaths = orderedArticlePages.ToDictionary(
                output => output.Url, output => output.OutputPath, StringComparer.Ordinal);
            foreach (var group in tagGroups)
            {
                var outputPath = tagPaths[group.Name];
                ArticleLink[] tagArticles =
                [
                    .. group.Articles.Select(article => article with
                    {
                        Url = PageUrl.GetRelative(outputPath, articlePaths[article.Url])
                    })
                ];
                var tagPage = new TagPage(
                    group.Name, tagArticles, CreateContext(outputPath),
                    PageUrl.GetRelative(outputPath, homePath), PageUrl.GetRelative(outputPath, tagsPath));
                var html = await pageRenderer.RenderAsync(tagPage, cancellationToken);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await File.WriteAllTextAsync(outputPath, html, cancellationToken);
            }
        }

        if (hasCategories)
        {
            var archive = new CategoryArchivePage(
                GroupByCategory(orderedArticles), CreateContext(categoriesPath), PageUrl.GetRelative(categoriesPath, homePath));
            var categoriesHtml = await pageRenderer.RenderAsync(archive, cancellationToken);
            await File.WriteAllTextAsync(categoriesPath, categoriesHtml, cancellationToken);
        }

        if (hasArticles)
        {
            var archive = new YearArchivePage(
                GroupByYear(orderedArticles), CreateContext(archivesPath), PageUrl.GetRelative(archivesPath, homePath));
            var archivesHtml = await pageRenderer.RenderAsync(archive, cancellationToken);
            await File.WriteAllTextAsync(archivesPath, archivesHtml, cancellationToken);
        }

        if (generateMetadata)
        {
            string[] urls =
            [
                .. reservedPaths.Concat(pagePaths)
                    .Select(path => PageUrl.GetAbsolute(path, outputDirectory, configuration.Url)!)
            ];
            await SitemapWriter.WriteAsync(sitemapPath, urls, cancellationToken);
            await RssWriter.WriteAsync(feedPath, configuration, orderedArticles, cancellationToken);
        }

        foreach (var alias in aliases)
        {
            var targetUrl = PageUrl.GetRelative(alias.OutputPath, alias.TargetPath);
            var canonicalUrl = generateMetadata
                ? PageUrl.GetAbsolute(alias.TargetPath, outputDirectory, configuration.Url)
                : null;
            await RedirectWriter.WriteAsync(
                alias.OutputPath, targetUrl, canonicalUrl, configuration.Language, cancellationToken);
        }

        await manifest.CompleteAsync(
            [.. contentPaths, .. reservedPaths, .. metadataPaths, stylesheetPath, .. assetPaths], cancellationToken);

        return sources.Count + reservedPaths.Count + aliases.Length;

        string? FindCopiedAsset(string relativePath) => assetPaths.FirstOrDefault(path =>
            string.Equals(path, Path.Combine(outputDirectory, relativePath), comparison));

        PageContext CreateContext(string pagePath, bool draft = false)
        {
            string[] stylesheetUrls = [.. stylesheetPaths.Select(path => PageUrl.GetRelative(pagePath, path))];
            NavigationLink[] navigation =
            [
                .. configuration.Navigation.Select(item => new NavigationLink(
                    item.Text, linkResolver.Resolve(item.Link, homePath, pagePath)))
            ];
            var canonicalUrl = draft ? null : PageUrl.GetAbsolute(pagePath, outputDirectory, configuration.Url);
            var feed = feedUrl is null ? null : new FeedLink(PageUrl.GetRelative(pagePath, feedPath), feedUrl);
            var faviconUrl = faviconPath is null ? null : PageUrl.GetRelative(pagePath, faviconPath);
            return new PageContext(configuration, stylesheetUrls, navigation, canonicalUrl, feed, faviconUrl);
        }
    }

    private static ArticleLink? GetAdjacentArticle(
        IReadOnlyList<ArticleOutput> pages, int index, string fromPagePath)
    {
        if (index < 0 || index >= pages.Count)
        {
            return null;
        }

        var adjacent = pages[index];
        return new ArticleLink(adjacent.Article.Info, PageUrl.GetRelative(fromPagePath, adjacent.OutputPath));
    }

    private async Task CopyStylesheetAsync(string outputPath, CancellationToken cancellationToken)
    {
        await using var source = stylesheet.OpenRead();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await using var target = File.Create(outputPath);
        await source.CopyToAsync(target, cancellationToken);
    }

    private static TagGroup[] GroupByTag(
        IReadOnlyList<ArticleLink> articles, string tagsPath, Dictionary<string, string> tagPaths) =>
    [
        .. articles.SelectMany(article => article.Info.Tags.Select(tag => (Tag: tag, Article: article)))
            .GroupBy(entry => entry.Tag, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new TagGroup(
                group.Key, [.. group.Select(entry => entry.Article)], PageUrl.GetRelative(tagsPath, tagPaths[group.Key])))
    ];

    private static CategoryGroup[] GroupByCategory(IReadOnlyList<ArticleLink> articles) =>
    [
        .. articles.SelectMany(article => article.Info.Categories.Select(category => (Category: category, Article: article)))
            .GroupBy(entry => entry.Category, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new CategoryGroup(group.Key, [.. group.Select(entry => entry.Article)]))
    ];

    private static YearGroup[] GroupByYear(IReadOnlyList<ArticleLink> articles) =>
    [
        .. articles.GroupBy(article => article.Info.Date?.Year)
            .OrderByDescending(group => group.Key)
            .Select(group => new YearGroup(group.Key, [.. group]))
    ];

    private async Task<List<(string Path, ParsedArticle Article)>> LoadArticlesAsync(
        IEnumerable<string> sourceFiles, CancellationToken cancellationToken)
    {
        var sources = new List<(string Path, ParsedArticle Article)>();
        foreach (var path in sourceFiles)
        {
            var markdown = await File.ReadAllTextAsync(path, cancellationToken);
            var article = articleParser.Parse(markdown, path);
            sources.Add((path, article));
        }

        return sources;
    }

    private sealed record ArticleOutput(ParsedArticle Article, string OutputPath, string Url, string OriginalPath);

    private sealed record AliasOutput(string OutputPath, string TargetPath);
}
