using RazorFolio.Core.Configuration;
using RazorFolio.Core.Content;
using RazorFolio.Core.Output;

namespace RazorFolio.Core.Rendering;

internal sealed class PageContextFactory(
    SiteConfiguration configuration,
    SiteBuildTarget target,
    ContentLinkResolver linkResolver,
    string homePath,
    PageContextFactory.AssetPaths assets,
    (string OutputPath, string AbsoluteUrl)? feed,
    IReadOnlyList<(string Text, string OutputPath)> browsePages,
    IReadOnlyList<(ArticleInfo Info, string OutputPath)> orderedArticles)
{
    private readonly (ArticleInfo Info, string OutputPath)[] starredArticles =
        orderedArticles.Where(article => article.Info.Star).ToArray();

    private readonly IGrouping<string?, (ArticleInfo Info, string OutputPath)>[] sidebarGroups = orderedArticles
        .SelectMany(article => article.Info.Categories.Count == 0
            ? [(Category: null, Article: article)]
            : article.Info.Categories.Select(category => (Category: (string?)category, Article: article)))
        .GroupBy(entry => entry.Category, entry => entry.Article, StringComparer.Ordinal)
        .OrderBy(group => group.Key is null)
        .ThenBy(group => group.Key, StringComparer.Ordinal)
        .ToArray();

    public PageContext Create(string pagePath, bool draft = false, bool rootRelative = false)
    {
        string[] stylesheetUrls = [.. assets.Stylesheets.Select(GetUrl)];
        string[] scriptUrls = [.. assets.Scripts.Select(GetUrl)];
        string[] contentScriptUrls = [.. assets.ContentScripts.Select(GetUrl)];
        NavigationLink[] navigation =
        [
            .. configuration.Navigation.Select(item => new NavigationLink(item.Text, ResolveConfiguredLink(item.Link)))
        ];
        NavigationLink[] browseLinks = [.. browsePages.Select(item => new NavigationLink(item.Text, GetUrl(item.OutputPath)))];
        var theme = ThemeView.Create(configuration, ResolveConfiguredLink);

        var canonicalUrl = draft || rootRelative
            ? null
            : PageUrl.GetAbsolute(pagePath, target.OutputDirectory, configuration.Url);
        var feedLink = feed is { } feedTarget ? new FeedLink(GetUrl(feedTarget.OutputPath), feedTarget.AbsoluteUrl) : null;
        var faviconUrl = assets.Favicon is { } faviconPath ? GetUrl(faviconPath) : null;

        return new PageContext(configuration, stylesheetUrls, scriptUrls, contentScriptUrls, navigation,
            browseLinks, GetUrl(homePath), GetUrl(pagePath), GetUrl(assets.SearchIndex),
            theme, canonicalUrl, feedLink, faviconUrl)
        {
            StarredArticles = [.. starredArticles.Select(CreateArticleLink)],
            SidebarCategories =
            [
                .. sidebarGroups.Select(group => new CategoryGroup(group.Key ?? "未分类", [.. group.Select(CreateArticleLink)]))
            ]
        };

        ArticleLink CreateArticleLink((ArticleInfo Info, string OutputPath) article) =>
            new(article.Info, GetUrl(article.OutputPath));

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

    internal sealed record AssetPaths(
        IReadOnlyList<string> Stylesheets,
        IReadOnlyList<string> Scripts,
        IReadOnlyList<string> ContentScripts,
        string SearchIndex,
        string? Favicon);
}
