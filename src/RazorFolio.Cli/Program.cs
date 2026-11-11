using RazorFolio.Cli.Commands;
using RazorFolio.Core;
using RazorFolio.Core.Rendering;

if (args.Length == 0 || (args.Length == 1 && args[0] is "--help" or "-h"))
{
    Console.WriteLine("""
        RazorFolio (rf)

        Usage:
          rf build --site <directory>

        Convert top-level content/*.md files to public/*.html.
        Output contains HTML fragments; Razor layouts are not connected yet.

        Planned commands: new, dev, preview.
        """);

    return 0;
}

try
{
    var command = BuildCommand.Parse(args);
    var builder = new SiteBuilder(new MarkdownRenderer());
    var pageCount = await builder.BuildAsync(command.SiteDirectory);

    Console.WriteLine($"Built {pageCount} page(s) in {Path.Combine(command.SiteDirectory, "public")}");
    return 0;
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Build failed: {exception.Message}");
    return 1;
}
