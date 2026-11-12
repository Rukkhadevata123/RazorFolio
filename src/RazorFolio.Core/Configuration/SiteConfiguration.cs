using System.Text.Json.Serialization;

namespace RazorFolio.Core.Configuration;

public sealed record SiteConfiguration
{
    public string Title { get; init; } = "RazorFolio";
    public string Language { get; init; } = "zh-CN";
    public int PageSize { get; init; } = 20;
    public IReadOnlyList<NavigationItem> Navigation { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }
}
