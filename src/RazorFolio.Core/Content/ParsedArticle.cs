using Markdig.Syntax;

namespace RazorFolio.Core.Content;

public sealed record ParsedArticle(string Title, MarkdownDocument Body, DateOnly? Date);
