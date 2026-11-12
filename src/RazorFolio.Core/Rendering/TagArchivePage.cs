namespace RazorFolio.Core.Rendering;

public sealed record TagArchivePage(
    IReadOnlyList<TagGroup> Groups,
    PageContext Context,
    string HomeUrl) : IPageModel;
