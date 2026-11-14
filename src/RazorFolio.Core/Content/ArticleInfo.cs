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
    public LicenseOptions? License { get; init; }
}
