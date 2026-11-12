namespace RazorFolio.Core.Content;

public sealed record ArticleInfo(string Title, DateOnly? Date, bool Draft, string? Description);
