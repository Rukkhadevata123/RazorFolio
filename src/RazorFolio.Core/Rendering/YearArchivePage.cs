namespace RazorFolio.Core.Rendering;

public sealed record YearArchivePage(
    IReadOnlyList<YearGroup> Groups,
    PageContext Context,
    string HomeUrl) : IPageModel;
