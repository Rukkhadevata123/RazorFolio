using System.Text.Json.Serialization;

namespace RazorFolio.Core.Configuration;

public sealed record SiteConfiguration
{
    public string Title { get; init; } = "RazorFolio";
    public string Language { get; init; } = "zh-CN";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }
}
