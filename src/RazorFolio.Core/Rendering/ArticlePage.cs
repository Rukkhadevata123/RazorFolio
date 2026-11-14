using RazorFolio.Core.Content;
using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record ArticlePage(
    ArticleInfo Info,
    string BodyHtml,
    string? TitleHtml,
    IReadOnlyList<HeadingLink> Headings,
    PageContext Context,
    string HomeUrl,
    IReadOnlyList<TagLink> Tags,
    IReadOnlyList<CategoryLink> Categories,
    ArticleNavigation Navigation) : IPageModel
{
    public LicenseOptions? License => Info.License ?? (Info.IsArticle ? Context.Site.License : null);
}
