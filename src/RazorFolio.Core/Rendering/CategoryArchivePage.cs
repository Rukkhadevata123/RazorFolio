namespace RazorFolio.Core.Rendering;

public sealed record CategoryArchivePage(
    IReadOnlyList<CategoryGroup> Groups,
    PageContext Context,
    string HomeUrl) : IPageModel;
