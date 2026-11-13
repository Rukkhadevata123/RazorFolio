namespace RazorFolio.Core.Rendering;

public sealed record HomePage(
    IReadOnlyList<ArticleLink> Articles,
    PageContext Context,
    Pagination Pagination) : IPageModel;
