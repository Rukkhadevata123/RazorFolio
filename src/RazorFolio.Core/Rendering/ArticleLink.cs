namespace RazorFolio.Core.Rendering;

public sealed record ArticleLink(string Title, string Url, DateOnly? Date, bool Draft);
