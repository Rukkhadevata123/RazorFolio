namespace RazorFolio.Core.Configuration;

public sealed class LicenseOptions
{
    public bool Enabled { get; set; } = true;
    public string? Name { get; set; }
    public string? Url { get; set; }
}

internal static class LicenseConfiguration
{
    public static LicenseOptions? Normalize(LicenseOptions? license, string sourcePath)
    {
        if (license is null) return null;
        if (!license.Enabled) return new LicenseOptions { Enabled = false };

        if (string.IsNullOrWhiteSpace(license.Name))
        {
            throw new InvalidDataException($"license.name in '{sourcePath}' must not be empty.");
        }

        string? url = null;
        if (license.Url is not null)
        {
            if (!Uri.TryCreate(license.Url.Trim(), UriKind.Absolute, out var target)
                || !target.IsWellFormedOriginalString() || target.Scheme is not ("http" or "https")
                || target.UserInfo.Length > 0)
            {
                throw new InvalidDataException($"license.url in '{sourcePath}' must be an absolute HTTP(S) URL without credentials.");
            }
            url = target.AbsoluteUri;
        }

        return new LicenseOptions { Name = license.Name.Trim(), Url = url };
    }
}
