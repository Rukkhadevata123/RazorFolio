namespace RazorFolio.Cli.Commands;

internal sealed record NewCommand(string SiteDirectory)
{
    public static NewCommand Parse(string[] args)
    {
        if (args.Length != 2 || args[0] != "new"
            || string.IsNullOrWhiteSpace(args[1]) || args[1].StartsWith('-'))
        {
            throw new ArgumentException("Usage: rf new <directory>");
        }

        return new NewCommand(Path.GetFullPath(args[1]));
    }
}
