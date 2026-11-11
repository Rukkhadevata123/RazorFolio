namespace RazorFolio.Core.Configuration;

public sealed record SiteConfiguration
{
    public string Title { get; init; } = "RazorFolio";
    public string Language { get; init; } = "zh-CN";
}
