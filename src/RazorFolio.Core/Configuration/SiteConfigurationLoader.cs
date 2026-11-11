using System.Text.Json;
using System.Text.Json.Serialization;

namespace RazorFolio.Core.Configuration;

public static class SiteConfigurationLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static async Task<SiteConfiguration> LoadAsync(
        string siteDirectory,
        CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(siteDirectory, "razorfolio.json");
        if (!File.Exists(path))
        {
            return new SiteConfiguration();
        }

        SiteConfiguration? configuration;
        try
        {
            await using var stream = File.OpenRead(path);
            configuration = await JsonSerializer.DeserializeAsync<SiteConfiguration>(stream, Options, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Invalid site configuration '{path}': {exception.Message}", exception);
        }

        if (configuration is null || string.IsNullOrWhiteSpace(configuration.Title)
            || string.IsNullOrWhiteSpace(configuration.Language))
        {
            throw new InvalidDataException($"Site configuration '{path}' must have non-empty title and language values.");
        }

        return configuration with { Title = configuration.Title.Trim(), Language = configuration.Language.Trim() };
    }
}
