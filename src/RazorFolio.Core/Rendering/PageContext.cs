using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record PageContext(
    SiteConfiguration Site,
    IReadOnlyList<string> StylesheetUrls,
    IReadOnlyList<NavigationLink> Navigation,
    string? CanonicalUrl,
    FeedLink? Feed,
    string? FaviconUrl);
