using RazorFolio.Core.Configuration;
using RazorFolio.Core.Content;

namespace RazorFolio.Core.Rendering;

public sealed record ArticlePage(
    ArticleInfo Info,
    string BodyHtml,
    SiteConfiguration Site,
    string HomeUrl,
    IReadOnlyList<TagLink> Tags,
    IReadOnlyList<string> StylesheetUrls,
    string? CanonicalUrl);
