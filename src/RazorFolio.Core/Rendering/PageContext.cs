using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record PageContext(
    SiteConfiguration Site,
    IReadOnlyList<string> StylesheetUrls,
    string? CanonicalUrl,
    FeedLink? Feed,
    string? FaviconUrl);
