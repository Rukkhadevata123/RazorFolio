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
    ThemeView Theme,
    string? CanonicalUrl,
    FeedLink? Feed,
    string? FaviconUrl);
