using RazorFolio.Core.Content;

namespace RazorFolio.Core.Rendering;

public sealed record ArticlePage(
    ArticleInfo Info,
    string BodyHtml,
    PageContext Context,
    string HomeUrl,
    IReadOnlyList<TagLink> Tags,
    IReadOnlyList<CategoryLink> Categories,
    ArticleNavigation Navigation) : IPageModel;
