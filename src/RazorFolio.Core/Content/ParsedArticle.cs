using Markdig.Syntax;

namespace RazorFolio.Core.Content;

public sealed record ParsedArticle(
    ArticleInfo Info, MarkdownDocument Body, string? OutputUrl, IReadOnlyList<string> Aliases);
