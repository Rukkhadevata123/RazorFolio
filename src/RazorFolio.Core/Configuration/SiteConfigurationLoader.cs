using System.Text.Json;
using System.Text.Json.Serialization;
using RazorFolio.Core.Serialization;

namespace RazorFolio.Core.Configuration;

public static class SiteConfigurationLoader
{
    private static readonly RazorFolioJsonContext Json = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    });

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
            configuration = await JsonSerializer.DeserializeAsync(stream, Json.SiteConfiguration, cancellationToken);
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

        if (configuration.PageSize <= 0)
        {
            throw new InvalidDataException($"Site configuration '{path}' must have a positive pageSize.");
        }

        return configuration with
        {
            Title = configuration.Title.Trim(),
            Language = configuration.Language.Trim(),
            Description = string.IsNullOrWhiteSpace(configuration.Description) ? null : configuration.Description.Trim(),
            Url = NormalizeUrl(configuration.Url, path),
            Navigation = NavigationConfiguration.Normalize(configuration.Navigation, path),
            Theme = ThemeConfiguration.Normalize(configuration.Theme, path)
        };
    }

    private static string? NormalizeUrl(string? value, string configurationPath)
    {
        if (value is null)
        {
            return null;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var url)
            || !url.IsWellFormedOriginalString()
            || url.Scheme is not ("http" or "https")
            || url.UserInfo.Length > 0 || url.Query.Length > 0 || url.Fragment.Length > 0)
        {
            throw new InvalidDataException(
                $"Invalid url in '{configurationPath}'. Use an absolute HTTP(S) site URL without credentials, query or fragment.");
        }

        // Treat the configured path as the site directory when resolving page URLs.
        return url.AbsoluteUri.EndsWith('/') ? url.AbsoluteUri : url.AbsoluteUri + "/";
    }
}
