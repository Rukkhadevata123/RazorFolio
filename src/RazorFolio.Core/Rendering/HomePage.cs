namespace RazorFolio.Core.Rendering;

public sealed record HomePage(
    IReadOnlyList<ArticleLink> Articles,
    PageContext Context,
    string? TagsUrl,
    string? CategoriesUrl,
    string? ArchivesUrl,
    Pagination Pagination) : IPageModel;
