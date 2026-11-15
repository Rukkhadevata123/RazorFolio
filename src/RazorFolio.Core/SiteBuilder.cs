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
        var configuration = await SiteConfigurationLoader.LoadAsync(siteRoot, target.UrlOverride, cancellationToken);
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
        var sources = await LoadArticlesAsync(sourceFiles, cancellationToken);
        var plan = new SiteOutputPlan(target, configuration, contentDirectory, sources);
        var homePath = plan.HomePath;
        var linkResolver = plan.LinkResolver;

        SearchDocument[] searchDocuments =
        [
            .. plan.ContentPages.Select(output => new SearchDocument(
                output.Article.Info.Title,
                output.Article.Info.Tags,
                MarkdownTextExtractor.Extract(output.Article.Body),
                PageUrl.GetRelative(homePath, output.OutputPath)))
        ];
        var searchIndexContent = SearchIndexWriter.Serialize(searchDocuments);
        var searchIndexPath = ContentFingerprint.AddToPath(plan.SearchIndexAliasPath, searchIndexContent);
        var customStylesheetSource = Path.Combine(siteRoot, "static", "custom.css");
        var customStylesheetContent = File.Exists(customStylesheetSource)
            ? await File.ReadAllBytesAsync(customStylesheetSource, cancellationToken)
            : null;
        var customStylesheetPath = customStylesheetContent is null ? null
            : ContentFingerprint.AddToPath(plan.CustomStylesheetBasePath, customStylesheetContent);
        var feedUrl = plan.GenerateMetadata ? PageUrl.GetAbsolute(plan.FeedPath, outputDirectory, configuration.Url) : null;
        var preparedThemeAssets = await ThemeAssetPreparation.PrepareAsync(themeAssets, outputDirectory, cancellationToken);
        string[] themeAssetPaths = [.. preparedThemeAssets.Select(asset => asset.OutputPath)];
        var generatedPaths = plan.ValidateGeneratedPaths(themeAssetPaths, searchIndexPath, customStylesheetPath);

        OutputFile.CreateDirectory(outputDirectory);
        string[] themeSourcePaths =
            [.. themeAssets.Select(asset => Path.GetFullPath(asset.RelativePath, outputDirectory))];
        var assetPaths = await assetCopier.CopyAsync(
            Path.Combine(siteRoot, "static"), outputDirectory,
            [.. generatedPaths, .. themeSourcePaths], cancellationToken);
        foreach (var asset in preparedThemeAssets)
        {
            await OutputFile.WriteBytesAsync(asset.OutputPath, asset.Content, cancellationToken);
        }

        if (customStylesheetPath is not null)
        {
            await OutputFile.WriteBytesAsync(customStylesheetPath, customStylesheetContent!, cancellationToken);
        }

        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var faviconOutputPath = Path.Combine(outputDirectory, "favicon.svg");
        var faviconPath = assetPaths.FirstOrDefault(path => string.Equals(path, faviconOutputPath, comparison));
        List<string> stylesheetPaths = [.. GetAssetPaths(ThemeAssetKind.Stylesheet)];
        if (customStylesheetPath is not null)
        {
            stylesheetPaths.Add(customStylesheetPath);
        }
        var scriptPaths = GetAssetPaths(ThemeAssetKind.Script);
        var contentScriptPaths = GetAssetPaths(ThemeAssetKind.ContentScript);

        ArticleOutput[] orderedArticlePages =
        [
            .. plan.ContentPages.Where(output => output.Article.Info.IsArticle)
                .OrderByDescending(output => output.Article.Info.Date)
                .ThenBy(output => output.Url, StringComparer.Ordinal)
        ];
        var navigations = orderedArticlePages.Select((output, index) => KeyValuePair.Create(
            output.OutputPath,
            new ArticleNavigation(
                GetAdjacentArticle(orderedArticlePages, index - 1, output.OutputPath),
                GetAdjacentArticle(orderedArticlePages, index + 1, output.OutputPath))))
            .ToDictionary();

        List<(string Text, string OutputPath)> browsePages = [];
        if (plan.HasArticles) browsePages.Add(("年份归档", plan.ArchivesPath));
        if (plan.HasCategories) browsePages.Add(("分类", plan.CategoriesPath));
        if (plan.HasTags) browsePages.Add(("标签", plan.TagsPath));

        var contextFactory = new PageContextFactory(
            configuration, target, linkResolver, homePath,
            new PageContextFactory.AssetPaths(stylesheetPaths, scriptPaths, contentScriptPaths, searchIndexPath, faviconPath),
            feedUrl is null ? null : (plan.FeedPath, feedUrl),
            browsePages,
            [.. orderedArticlePages.Select(output => (output.Article.Info, output.OutputPath))]);

        var pageAnchors = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var output in plan.ContentPages)
        {
            var article = output.Article;
            var outputPath = output.OutputPath;
            var homeUrl = PageUrl.GetRelative(outputPath, homePath);
            TagLink[] tags = article.Info.IsArticle ?
            [
                .. article.Info.Tags.Select(tag => new TagLink(tag, PageUrl.GetRelative(outputPath, plan.TagPaths[tag])))
            ] : [];
            var categoriesUrl = PageUrl.GetRelative(outputPath, plan.CategoriesPath);
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
                contextFactory.Create(outputPath, article.Info.Draft), homeUrl, tags, categories, navigation);
            var html = await pageRenderer.RenderAsync(page, cancellationToken);
            if (reportDiagnostic is not null)
            {
                pageAnchors.Add(GetRelativeOutputPath(outputPath), HtmlAnchorReader.Read(html));
            }

            await OutputFile.WriteTextAsync(outputPath, html, cancellationToken);
        }

        ArticleLink[] orderedArticles =
        [
            .. orderedArticlePages.Select(output => CreateArticleLink(output.Article.Info, output.OutputPath, homePath))
        ];
        var homePaths = plan.HomePaths;
        for (var index = 0; index < homePaths.Count; index++)
        {
            var outputPath = homePaths[index];
            ArticleLink[] pageArticles =
            [
                .. orderedArticlePages.Skip(index * configuration.PageSize).Take(configuration.PageSize)
                    .Select(output => CreateArticleLink(output.Article.Info, output.OutputPath, outputPath))
            ];
            var pagination = new Pagination(
                index + 1,
                homePaths.Count,
                index > 0 ? PageUrl.GetRelative(outputPath, homePaths[index - 1]) : null,
                index + 1 < homePaths.Count ? PageUrl.GetRelative(outputPath, homePaths[index + 1]) : null);
            var home = new HomePage(
                pageArticles, contextFactory.Create(outputPath), pagination);
            var homeHtml = await pageRenderer.RenderAsync(home, cancellationToken);

            await OutputFile.WriteTextAsync(outputPath, homeHtml, cancellationToken);
        }

        if (plan.HasTags)
        {
            var tagGroups = GroupByTag(orderedArticles, plan.TagsPath, plan.TagPaths);
            var archive = new TagArchivePage(
                tagGroups, contextFactory.Create(plan.TagsPath), PageUrl.GetRelative(plan.TagsPath, homePath));
            var tagsHtml = await pageRenderer.RenderAsync(archive, cancellationToken);
            await OutputFile.WriteTextAsync(plan.TagsPath, tagsHtml, cancellationToken);

            var articlePaths = orderedArticlePages.ToDictionary(
                output => output.Url, output => output.OutputPath, StringComparer.Ordinal);
            foreach (var group in tagGroups)
            {
                var outputPath = plan.TagPaths[group.Name];
                ArticleLink[] tagArticles =
                [
                    .. group.Articles.Select(article => CreateArticleLink(article.Info, articlePaths[article.Url], outputPath))
                ];
                var tagPage = new TagPage(
                    group.Name, tagArticles, contextFactory.Create(outputPath),
                    PageUrl.GetRelative(outputPath, homePath), PageUrl.GetRelative(outputPath, plan.TagsPath));
                var html = await pageRenderer.RenderAsync(tagPage, cancellationToken);
                await OutputFile.WriteTextAsync(outputPath, html, cancellationToken);
            }
        }

        if (plan.HasCategories)
        {
            var archive = new CategoryArchivePage(
                GroupByCategory(orderedArticles), contextFactory.Create(plan.CategoriesPath),
                PageUrl.GetRelative(plan.CategoriesPath, homePath));
            var categoriesHtml = await pageRenderer.RenderAsync(archive, cancellationToken);
            await OutputFile.WriteTextAsync(plan.CategoriesPath, categoriesHtml, cancellationToken);
        }

        if (plan.HasArticles)
        {
            var archive = new YearArchivePage(
                GroupByYear(orderedArticles), contextFactory.Create(plan.ArchivesPath),
                PageUrl.GetRelative(plan.ArchivesPath, homePath));
            var archivesHtml = await pageRenderer.RenderAsync(archive, cancellationToken);
            await OutputFile.WriteTextAsync(plan.ArchivesPath, archivesHtml, cancellationToken);
        }

        var notFoundPage = new NotFoundPage(contextFactory.Create(plan.NotFoundPath, rootRelative: true));
        var notFoundHtml = await pageRenderer.RenderAsync(notFoundPage, cancellationToken);
        await OutputFile.WriteTextAsync(plan.NotFoundPath, notFoundHtml, cancellationToken);

        if (plan.GenerateMetadata)
        {
            SitemapEntry[] entries =
            [
                .. plan.ListingPaths.Select(path => new SitemapEntry(
                    PageUrl.GetAbsolute(path, outputDirectory, configuration.Url)!)),
                .. plan.ContentPages.Select(output => new SitemapEntry(
                    PageUrl.GetAbsolute(output.OutputPath, outputDirectory, configuration.Url)!,
                    output.Article.Info.Updated ?? output.Article.Info.Date))
            ];
            await SitemapWriter.WriteAsync(plan.SitemapPath, entries, cancellationToken);
            await RssWriter.WriteAsync(plan.FeedPath, configuration, orderedArticles, cancellationToken);
        }

        foreach (var alias in plan.Aliases)
        {
            var targetUrl = PageUrl.GetRelative(alias.OutputPath, alias.TargetPath);
            var canonicalUrl = plan.GenerateMetadata
                ? PageUrl.GetAbsolute(alias.TargetPath, outputDirectory, configuration.Url)
                : null;
            await RedirectWriter.WriteAsync(
                alias.OutputPath, targetUrl, canonicalUrl, configuration.Language, cancellationToken);
        }

        await OutputFile.WriteBytesAsync(searchIndexPath, searchIndexContent, cancellationToken);
        // Keep the stable endpoint for external consumers; pages always use the fingerprinted URL.
        await SearchIndexWriter.WriteAsync(plan.SearchIndexAliasPath, searchDocuments, cancellationToken);

        if (reportDiagnostic is not null)
        {
            foreach (var alias in plan.Aliases)
            {
                pageAnchors.Add(GetRelativeOutputPath(alias.OutputPath),
                    pageAnchors[GetRelativeOutputPath(alias.TargetPath)]);
            }

            var checker = new ContentLinkChecker(outputDirectory,
                [.. generatedPaths, .. assetPaths], configuration.Url, pageAnchors);
            foreach (var output in plan.ContentPages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var diagnostic in checker.Check(output.Article.Body, output.SourcePath,
                    output.OriginalPath, output.OutputPath, linkResolver))
                {
                    reportDiagnostic(diagnostic);
                }
            }
        }

        await manifest.CompleteAsync([.. generatedPaths, .. assetPaths], cancellationToken);
        return plan.PageCount;

        string GetRelativeOutputPath(string path) =>
            Path.GetRelativePath(outputDirectory, path).Replace(Path.DirectorySeparatorChar, '/');

        ArticleLink CreateArticleLink(ArticleInfo info, string targetPath, string fromPath) =>
            new(info, PageUrl.GetRelative(fromPath, targetPath))
            {
                Tags = [.. info.Tags.Select(tag => new TagLink(tag, PageUrl.GetRelative(fromPath, plan.TagPaths[tag])))],
                Categories =
                [
                    .. info.Categories.Select(category => new CategoryLink(
                        category, PageUrl.GetRelative(fromPath, plan.CategoriesPath) + "#" + CategoryGroup.GetId(category)))
                ]
            };

        string[] GetAssetPaths(ThemeAssetKind kind) =>
        [
            .. themeAssets.Select((asset, index) => (asset, index))
                .Where(entry => entry.asset.Kind == kind).Select(entry => themeAssetPaths[entry.index])
        ];
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

    private static TagGroup[] GroupByTag(
        IReadOnlyList<ArticleLink> articles, string tagsPath, IReadOnlyDictionary<string, string> tagPaths) =>
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
}
