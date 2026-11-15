using RazorFolio.Core.Output;

namespace RazorFolio.Core.Rendering;

internal static class RelatedArticleSelector
{
    private const int MaxRelatedArticles = 3;

    public static IReadOnlyList<ArticleLink> Select(ArticleOutput current, IReadOnlyList<ArticleOutput> orderedArticles)
    {
        var info = current.Article.Info;
        if (!info.IsArticle || (info.Tags.Count == 0 && info.Categories.Count == 0))
        {
            return [];
        }

        var tags = info.Tags.ToHashSet(StringComparer.Ordinal);
        var categories = info.Categories.ToHashSet(StringComparer.Ordinal);
        return
        [
            .. orderedArticles.Where(candidate => candidate.OutputPath != current.OutputPath)
                .Select(candidate => (
                    Candidate: candidate,
                    SharedTags: candidate.Article.Info.Tags.Count(tags.Contains),
                    SharesCategory: candidate.Article.Info.Categories.Any(categories.Contains)))
                .Where(match => match.SharedTags > 0 || match.SharesCategory)
                // The sort keeps the existing article order when both scores are equal.
                .OrderByDescending(match => match.SharedTags)
                .ThenByDescending(match => match.SharesCategory)
                .Take(MaxRelatedArticles)
                .Select(match => new ArticleLink(
                    match.Candidate.Article.Info, PageUrl.GetRelative(current.OutputPath, match.Candidate.OutputPath)))
        ];
    }
}
