using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Content;

public sealed class ArticleMetadata
{
    public string? Title { get; set; }
    public LicenseOptions? License { get; set; }
    public string? Description { get; set; }
    public string? ShareImage { get; set; }
    public string? Url { get; set; }
    public List<string?>? Aliases { get; set; }
    public string? Date { get; set; }
    public string? Updated { get; set; }
    public bool Draft { get; set; }
    public bool Star { get; set; }
    public bool Article { get; set; } = true;
    public List<string?>? Tags { get; set; }
    public List<string?>? Category { get; set; }
}
