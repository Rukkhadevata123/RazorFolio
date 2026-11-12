using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record ArticlePage(
    string Title,
    string BodyHtml,
    SiteConfiguration Site,
    DateOnly? Date,
    bool Draft,
    string HomeUrl);
