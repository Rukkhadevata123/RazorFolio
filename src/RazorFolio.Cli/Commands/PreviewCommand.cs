namespace RazorFolio.Cli.Commands;

internal sealed record PreviewCommand(string SiteDirectory, string Directory, int Port)
{
    public static PreviewCommand Parse(string[] args)
    {
        const string usage = "Usage: rf preview --site <directory> [--directory public] [--port 8080]";
        string? site = null;
        var directory = "public";
        var port = 8080;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 1; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !seen.Add(args[index]))
            {
                throw new ArgumentException(usage);
            }

            var value = args[index + 1];
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith('-'))
            {
                throw new ArgumentException(usage);
            }

            switch (args[index])
            {
                case "--site":
                    site = value;
                    break;
                case "--directory":
                    directory = value;
                    break;
                case "--port":
                    if (!int.TryParse(value, out port) || port is < 1 or > 65535)
                    {
                        throw new ArgumentException("Port must be an integer between 1 and 65535.");
                    }
                    break;
                default:
                    throw new ArgumentException(usage);
            }
        }

        if (site is null)
        {
            throw new ArgumentException(usage);
        }

        var siteDirectory = Path.GetFullPath(site);
        return new PreviewCommand(siteDirectory, Path.GetFullPath(directory, siteDirectory), port);
    }
}
