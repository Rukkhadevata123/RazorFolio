namespace RazorFolio.Core.Content;

public sealed class ArticleMetadata
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Date { get; set; }
    public bool Draft { get; set; }
    public List<string?>? Tags { get; set; }
}
