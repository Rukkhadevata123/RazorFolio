namespace RazorFolio.Core.Output;

public sealed record SitemapEntry(string Url, DateOnly? LastModified = null);
