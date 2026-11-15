namespace RazorFolio.Cli.Commands;

internal sealed record SiteCommand(string Name, string SiteDirectory, int Port, string? UrlOverride, bool StrictLinks)
{
    private const string Usage =
        "Usage: rf build --site <directory> [--url <site-url>] [--strict-links] | rf dev --site <directory> [--port 8080]";

    public static SiteCommand Parse(string[] args)
    {
        if (args.Length < 3 || args[0] is not ("build" or "dev") || args[1] != "--site"
            || string.IsNullOrWhiteSpace(args[2]) || args[2].StartsWith('-'))
        {
            throw new ArgumentException(Usage);
        }

        var port = 8080;
        if (args[0] == "dev")
        {
            if (args.Length != 3 && (args.Length != 5 || args[3] != "--port"
                || !int.TryParse(args[4], out port) || port is < 1 or > 65535))
            {
                throw new ArgumentException("Dev accepts --port followed by an integer between 1 and 65535.");
            }

            return new SiteCommand("dev", Path.GetFullPath(args[2]), port, UrlOverride: null, StrictLinks: false);
        }

        string? urlOverride = null;
        var strictLinks = false;
        for (var index = 3; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--url" when urlOverride is null && index + 1 < args.Length
                    && !string.IsNullOrWhiteSpace(args[index + 1]) && !args[index + 1].StartsWith('-'):
                    urlOverride = args[++index];
                    break;
                case "--strict-links" when !strictLinks:
                    strictLinks = true;
                    break;
                default:
                    throw new ArgumentException(Usage);
            }
        }

        return new SiteCommand("build", Path.GetFullPath(args[2]), port, urlOverride, strictLinks);
    }
}
