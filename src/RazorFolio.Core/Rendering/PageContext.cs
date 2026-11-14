using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record PageContext(
    SiteConfiguration Site,
    IReadOnlyList<string> StylesheetUrls,
    IReadOnlyList<string> ScriptUrls,
    IReadOnlyList<string> ContentScriptUrls,
    IReadOnlyList<NavigationLink> Navigation,
    IReadOnlyList<NavigationLink> BrowseLinks,
    string HomeUrl,
    string CurrentUrl,
    string SearchIndexUrl,
    ThemeView Theme,
    string? CanonicalUrl,
    FeedLink? Feed,
    string? FaviconUrl)
{
    public IReadOnlyList<ArticleLink> StarredArticles { get; init; } = [];
    public IReadOnlyList<CategoryGroup> SidebarCategories { get; init; } = [];
}
