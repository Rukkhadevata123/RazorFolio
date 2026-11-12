namespace RazorFolio.Core.Rendering;

public sealed record TagPage(
    string Name,
    IReadOnlyList<ArticleLink> Articles,
    PageContext Context,
    string HomeUrl,
    string TagsUrl) : IPageModel;
