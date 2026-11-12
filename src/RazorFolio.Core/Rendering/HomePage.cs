using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record HomePage(
    IReadOnlyList<ArticleLink> Articles,
    SiteConfiguration Site,
    string? TagsUrl,
    IReadOnlyList<string> StylesheetUrls,
    string? CanonicalUrl,
    FeedLink? Feed);
