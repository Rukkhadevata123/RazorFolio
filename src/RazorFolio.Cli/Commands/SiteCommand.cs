namespace RazorFolio.Cli.Commands;

internal sealed record SiteCommand(string Name, string SiteDirectory, int Port)
{
    public static SiteCommand Parse(string[] args)
    {
        if (args.Length is not (3 or 5) || args[0] is not ("build" or "dev") || args[1] != "--site"
            || string.IsNullOrWhiteSpace(args[2]) || args[2].StartsWith('-'))
        {
            throw new ArgumentException("Usage: rf build --site <directory> | rf dev --site <directory> [--port 8080]");
        }

        var port = 8080;
        if (args.Length == 5 && (args[0] != "dev" || args[3] != "--port"
            || !int.TryParse(args[4], out port) || port is < 1 or > 65535))
        {
            throw new ArgumentException("Only dev accepts --port, with an integer between 1 and 65535.");
        }

        return new SiteCommand(args[0], Path.GetFullPath(args[2]), port);
    }
}
