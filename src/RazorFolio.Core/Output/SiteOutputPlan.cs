using System.Globalization;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Content;

namespace RazorFolio.Core.Output;

internal sealed class SiteOutputPlan
{
    private readonly string[] contentPaths;

    public SiteOutputPlan(
        SiteBuildTarget target,
        SiteConfiguration configuration,
        string contentDirectory,
        IReadOnlyList<(string Path, ParsedArticle Article)> sources)
    {
        var outputDirectory = target.OutputDirectory;
        HomePath = Path.Combine(outputDirectory, "index.html");
        NotFoundPath = Path.Combine(outputDirectory, "404.html");
        TagsPath = Path.Combine(outputDirectory, "tags.html");
        CategoriesPath = Path.Combine(outputDirectory, "categories.html");
        ArchivesPath = Path.Combine(outputDirectory, "archives.html");
        SitemapPath = Path.Combine(outputDirectory, "sitemap.xml");
        FeedPath = Path.Combine(outputDirectory, "feed.xml");
        SearchIndexAliasPath = Path.Combine(outputDirectory, "search-index.json");
        CustomStylesheetBasePath = Path.Combine(outputDirectory, "custom.css");
        GenerateMetadata = configuration.Url is not null && !target.IncludeDrafts;

        var includedArticles = sources
            .Where(source => source.Article.Info.IsArticle && (target.IncludeDrafts || !source.Article.Info.Draft))
            .ToArray();
        var articleCount = includedArticles.Length;
        HasArticles = articleCount > 0;
        var homePageCount = articleCount == 0 ? 1 : 1 + (articleCount - 1) / configuration.PageSize;
        HomePaths =
        [
            .. Enumerable.Range(1, homePageCount).Select(number => number == 1
                ? HomePath
                : Path.Combine(outputDirectory, "page", number.ToString(CultureInfo.InvariantCulture) + ".html"))
        ];

        ArticleOutput[] allOutputs =
        [
            .. sources.Select(source =>
            {
                var originalPath = Path.Combine(outputDirectory, Path.GetRelativePath(contentDirectory, source.Path));
                var outputPath = source.Article.OutputUrl is { } customUrl
                    ? Path.Combine(outputDirectory, customUrl.Replace('/', Path.DirectorySeparatorChar))
                    : Path.ChangeExtension(originalPath, ".html");
                return new ArticleOutput(
                    source.Article, outputPath, PageUrl.GetRelative(HomePath, outputPath), originalPath, source.Path);
            })
        ];
        ContentPages = [.. allOutputs.Where(output => target.IncludeDrafts || !output.Article.Info.Draft)];

        // Keep draft paths in the resolver so links to relocated drafts can still be diagnosed correctly.
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        LinkResolver = new ContentLinkResolver(allOutputs.ToDictionary(
            output => output.OriginalPath, output => output.OutputPath, pathComparer));
        Aliases =
        [
            .. ContentPages.SelectMany(output => output.Article.Aliases.Select(alias => new AliasOutput(
                Path.Combine(outputDirectory, alias.Replace('/', Path.DirectorySeparatorChar)), output.OutputPath)))
        ];
        contentPaths = [.. ContentPages.Select(output => output.OutputPath), .. Aliases.Select(alias => alias.OutputPath)];
        OutputPathValidator.Validate(contentPaths);

        TagPaths = includedArticles.SelectMany(source => source.Article.Info.Tags)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(tag => tag, tag => TagPagePath.GetOutputPath(outputDirectory, tag), StringComparer.Ordinal);
        HasCategories = includedArticles.Any(source => source.Article.Info.Categories.Count > 0);

        List<string> listingPaths = [.. HomePaths];
        if (HasTags)
        {
            listingPaths.Add(TagsPath);
            listingPaths.AddRange(TagPaths.Values);
        }
        if (HasCategories)
        {
            listingPaths.Add(CategoriesPath);
        }
        if (HasArticles)
        {
            listingPaths.Add(ArchivesPath);
        }
        ListingPaths = listingPaths;
    }

    public string HomePath { get; }
    public string NotFoundPath { get; }
    public string TagsPath { get; }
    public string CategoriesPath { get; }
    public string ArchivesPath { get; }
    public string SitemapPath { get; }
    public string FeedPath { get; }
    public string SearchIndexAliasPath { get; }
    public string CustomStylesheetBasePath { get; }
    public bool GenerateMetadata { get; }
    public bool HasArticles { get; }
    public bool HasCategories { get; }
    public bool HasTags => TagPaths.Count > 0;

    public IReadOnlyList<string> HomePaths { get; }
    public IReadOnlyDictionary<string, string> TagPaths { get; }
    public IReadOnlyList<string> ListingPaths { get; }
    public IReadOnlyList<ArticleOutput> ContentPages { get; }
    public IReadOnlyList<AliasOutput> Aliases { get; }
    public ContentLinkResolver LinkResolver { get; }
    public int PageCount => ContentPages.Count + ListingPaths.Count + Aliases.Count + 1;

    public string[] ValidateGeneratedPaths(
        IReadOnlyList<string> themeAssetPaths, string searchIndexPath, string? customStylesheetPath)
    {
        string[] metadataPaths = GenerateMetadata ? [SitemapPath, FeedPath] : [];
        string[] fingerprintedDataPaths = customStylesheetPath is null
            ? [searchIndexPath, SearchIndexAliasPath]
            : [searchIndexPath, SearchIndexAliasPath, customStylesheetPath];
        string[] reservedPaths =
            [.. ListingPaths, .. metadataPaths, .. themeAssetPaths, .. fingerprintedDataPaths, NotFoundPath];

        OutputPathValidator.Validate(reservedPaths);
        OutputPathValidator.ValidateReservedPaths(contentPaths, reservedPaths);
        return [.. contentPaths, .. reservedPaths];
    }
}

internal sealed record ArticleOutput(
    ParsedArticle Article, string OutputPath, string Url, string OriginalPath, string SourcePath);

internal sealed record AliasOutput(string OutputPath, string TargetPath);
