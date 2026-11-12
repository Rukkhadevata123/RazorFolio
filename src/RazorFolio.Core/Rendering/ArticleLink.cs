using RazorFolio.Core.Content;

namespace RazorFolio.Core.Rendering;

public sealed record ArticleLink(ArticleInfo Info, string Url)
{
    public IReadOnlyList<TagLink> Tags { get; init; } = [];
    public IReadOnlyList<CategoryLink> Categories { get; init; } = [];
}
