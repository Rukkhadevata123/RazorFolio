using System.Globalization;
using RazorFolio.Core.Assets;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Content;
using RazorFolio.Core.Diagnostics;
using RazorFolio.Core.Output;
using RazorFolio.Core.Rendering;

namespace RazorFolio.Core;

public sealed class SiteBuilder(
    ArticleParser articleParser,
    MarkdownRenderer markdownRenderer,
    IPageRenderer pageRenderer,
    StaticAssetCopier assetCopier,
    IReadOnlyList<IThemeAsset> themeAssets,
    Action<ContentLinkDiagnostic>? reportDiagnostic = null)
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
        var notFoundPath = Path.Combine(outputDirectory, "404.html");
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
                return new ArticleOutput(
                    source.Article, outputPath, PageUrl.GetRelative(homePath, outputPath), originalPath, source.Path);
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
        var searchIndexPath = Path.Combine(outputDirectory, "search-index.json");
        var feedUrl = generateMetadata ? PageUrl.GetAbsolute(feedPath, outputDirectory, configuration.Url) : null;
        string[] metadataPaths = generateMetadata ? [sitemapPath, feedPath] : [];
        string[] themeAssetPaths =
        [
            .. themeAssets.Select(asset => Path.GetFullPath(asset.RelativePath, outputDirectory))
        ];
        string[] protectedPaths = [.. reservedPaths, .. metadataPaths, .. themeAssetPaths, searchIndexPath, notFoundPath];
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
        for (var index = 0; index < themeAssets.Count; index++)
        {
            await CopyThemeAssetAsync(themeAssets[index], themeAssetPaths[index], cancellationToken);
        }

        var customStylesheetPath = FindCopiedAsset("custom.css");
        var faviconPath = FindCopiedAsset("favicon.svg");
        List<string> stylesheetPaths = [.. GetAssetPaths(ThemeAssetKind.Stylesheet)];
        if (customStylesheetPath is not null)
        {
            stylesheetPaths.Add(customStylesheetPath);
        }
        var scriptPaths = GetAssetPaths(ThemeAssetKind.Script);
        var contentScriptPaths = GetAssetPaths(ThemeAssetKind.ContentScript);

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

        var pageAnchors = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
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
            var rendered = markdownRenderer.Render(article.Body,
                link => linkResolver.Resolve(link, output.OriginalPath, outputPath));
            var page = new ArticlePage(
                article.Info, rendered.BodyHtml, rendered.TitleHtml, rendered.Headings,
                CreateContext(outputPath, article.Info.Draft), homeUrl, tags, categories, navigation);
            var html = await pageRenderer.RenderAsync(page, cancellationToken);
            if (reportDiagnostic is not null)
            {
                pageAnchors.Add(GetRelativeOutputPath(outputPath), HtmlAnchorReader.Read(html));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, html, cancellationToken);
        }

        ArticleLink[] orderedArticles =
        [
            .. orderedArticlePages.Select(output => CreateArticleLink(output.Article.Info, output.OutputPath, homePath))
        ];
        for (var index = 0; index < homePaths.Length; index++)
        {
            var outputPath = homePaths[index];
            ArticleLink[] pageArticles =
            [
                .. orderedArticlePages.Skip(index * configuration.PageSize).Take(configuration.PageSize)
                    .Select(output => CreateArticleLink(output.Article.Info, output.OutputPath, outputPath))
            ];
            var pagination = new Pagination(
                index + 1,
                homePaths.Length,
                index > 0 ? PageUrl.GetRelative(outputPath, homePaths[index - 1]) : null,
                index + 1 < homePaths.Length ? PageUrl.GetRelative(outputPath, homePaths[index + 1]) : null);
            var home = new HomePage(
                pageArticles, CreateContext(outputPath), pagination);
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
                    .. group.Articles.Select(article => CreateArticleLink(article.Info, articlePaths[article.Url], outputPath))
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

        var notFoundPage = new NotFoundPage(CreateContext(notFoundPath, rootRelative: true));
        var notFoundHtml = await pageRenderer.RenderAsync(notFoundPage, cancellationToken);
        await File.WriteAllTextAsync(notFoundPath, notFoundHtml, cancellationToken);

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

        SearchDocument[] searchDocuments =
        [
            .. outputs.Select(output => new SearchDocument(
                output.Article.Info.Title,
                output.Article.Info.Tags,
                MarkdownTextExtractor.Extract(output.Article.Body),
                PageUrl.GetRelative(searchIndexPath, output.OutputPath)))
        ];
        await SearchIndexWriter.WriteAsync(searchIndexPath, searchDocuments, cancellationToken);

        if (reportDiagnostic is not null)
        {
            foreach (var alias in aliases)
            {
                pageAnchors.Add(GetRelativeOutputPath(alias.OutputPath),
                    pageAnchors[GetRelativeOutputPath(alias.TargetPath)]);
            }

            var checker = new ContentLinkChecker(outputDirectory,
                [.. contentPaths, .. protectedPaths, .. assetPaths], configuration.Url, pageAnchors);
            foreach (var output in outputs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var diagnostic in checker.Check(output.Article.Body, output.SourcePath,
                    output.OriginalPath, output.OutputPath, linkResolver))
                {
                    reportDiagnostic(diagnostic);
                }
            }
        }

        await manifest.CompleteAsync(
            [.. contentPaths, .. reservedPaths, .. metadataPaths, .. themeAssetPaths, .. assetPaths, searchIndexPath, notFoundPath],
            cancellationToken);

        return sources.Count + reservedPaths.Count + aliases.Length + 1;

        string GetRelativeOutputPath(string path) =>
            Path.GetRelativePath(outputDirectory, path).Replace(Path.DirectorySeparatorChar, '/');

        string? FindCopiedAsset(string relativePath) => assetPaths.FirstOrDefault(path =>
            string.Equals(path, Path.Combine(outputDirectory, relativePath), comparison));

        ArticleLink CreateArticleLink(ArticleInfo info, string targetPath, string fromPath) =>
            new(info, PageUrl.GetRelative(fromPath, targetPath))
            {
                Tags = [.. info.Tags.Select(tag => new TagLink(tag, PageUrl.GetRelative(fromPath, tagPaths[tag])))],
                Categories =
                [
                    .. info.Categories.Select(category => new CategoryLink(
                        category, PageUrl.GetRelative(fromPath, categoriesPath) + "#" + CategoryGroup.GetId(category)))
                ]
            };

        string[] GetAssetPaths(ThemeAssetKind kind) =>
        [
            .. themeAssets.Select((asset, index) => (asset, index))
                .Where(entry => entry.asset.Kind == kind).Select(entry => themeAssetPaths[entry.index])
        ];

        PageContext CreateContext(string pagePath, bool draft = false, bool rootRelative = false)
        {
            string[] stylesheetUrls = [.. stylesheetPaths.Select(GetUrl)];
            string[] scriptUrls = [.. scriptPaths.Select(GetUrl)];
            string[] contentScriptUrls = [.. contentScriptPaths.Select(GetUrl)];
            NavigationLink[] navigation =
            [
                .. configuration.Navigation.Select(item => new NavigationLink(
                    item.Text, ResolveConfiguredLink(item.Link)))
            ];
            var canonicalUrl = draft || rootRelative ? null : PageUrl.GetAbsolute(pagePath, outputDirectory, configuration.Url);
            var feed = feedUrl is null ? null : new FeedLink(GetUrl(feedPath), feedUrl);
            var faviconUrl = faviconPath is null ? null : GetUrl(faviconPath);
            List<NavigationLink> browseLinks = [];
            if (hasArticles) browseLinks.Add(new NavigationLink("年份归档", GetUrl(archivesPath)));
            if (hasCategories) browseLinks.Add(new NavigationLink("分类", GetUrl(categoriesPath)));
            if (hasTags) browseLinks.Add(new NavigationLink("标签", GetUrl(tagsPath)));
            var theme = ThemeView.Create(configuration, ResolveConfiguredLink);
            return new PageContext(configuration, stylesheetUrls, scriptUrls, contentScriptUrls, navigation,
                browseLinks, GetUrl(homePath), GetUrl(pagePath), GetUrl(searchIndexPath),
                theme, canonicalUrl, feed, faviconUrl);

            string GetUrl(string path) => ResolveUrl(PageUrl.GetRelative(pagePath, path));

            string ResolveConfiguredLink(string link) => ResolveUrl(linkResolver.Resolve(link, homePath, pagePath));

            string ResolveUrl(string url)
            {
                if (!rootRelative || url.StartsWith('/') || Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    return url;
                }

                // A 404 is served at the missing URL, so relative paths would depend on that URL's depth.
                var siteUrl = target.IncludeDrafts ? null : configuration.Url;
                var resolved = new Uri(new Uri(siteUrl ?? "https://razorfolio.invalid/"), url);
                return resolved.PathAndQuery + resolved.Fragment;
            }
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

    private static async Task CopyThemeAssetAsync(
        IThemeAsset asset, string outputPath, CancellationToken cancellationToken)
    {
        await using var source = asset.OpenRead();
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

    private sealed record ArticleOutput(
        ParsedArticle Article, string OutputPath, string Url, string OriginalPath, string SourcePath);

    private sealed record AliasOutput(string OutputPath, string TargetPath);
}
