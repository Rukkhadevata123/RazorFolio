using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Content;

public sealed record ArticleInfo(
    string Title,
    DateOnly? Date,
    bool Draft,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Categories,
    bool IsArticle)
{
    public string? ShareImage { get; init; }
    public DateOnly? Updated { get; init; }
    public int ReadingTimeMinutes { get; init; }
    public bool Star { get; init; }
    public LicenseOptions? License { get; init; }
}
