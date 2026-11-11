namespace RazorFolio.Cli.Commands;

internal sealed record BuildCommand(string SiteDirectory)
{
    public static BuildCommand Parse(string[] args)
    {
        if (args.Length != 3 || args[0] != "build" || args[1] != "--site"
            || string.IsNullOrWhiteSpace(args[2]) || args[2].StartsWith('-'))
        {
            throw new ArgumentException("Usage: rf build --site <directory>");
        }

        return new BuildCommand(Path.GetFullPath(args[2]));
    }
}
