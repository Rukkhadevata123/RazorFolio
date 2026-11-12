using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record TagArchivePage(
    IReadOnlyList<TagGroup> Groups,
    SiteConfiguration Site,
    string HomeUrl);
