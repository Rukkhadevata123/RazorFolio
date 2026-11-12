using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RazorFolio.Cli.Commands;
using RazorFolio.Cli.Hosting;
using RazorFolio.Core;
using RazorFolio.Core.Assets;
using RazorFolio.Core.Content;
using RazorFolio.Core.Rendering;
using RazorFolio.Themes.Default;

if (args.Length == 0 || (args.Length == 1 && args[0] is "--help" or "-h"))
{
    Console.WriteLine("""
        RazorFolio (rf)

        Usage:
          rf new <directory>
          rf build --site <directory>
          rf dev --site <directory> [--port 8080]
          rf preview --site <directory> [--directory public] [--port 8080]

        Convert content/**/*.md files to public/**/*.html, preserving subdirectories.
        Render complete HTML pages using the precompiled default Razor theme.

        Preview serves existing files on 127.0.0.1 without rebuilding them.

        Dev rebuilds on source changes and reloads connected browsers.

        New creates a minimal site in a new or empty directory.
        """);

    return 0;
}

try
{
    if (args[0] == "new")
    {
        var newSite = NewCommand.Parse(args);
        await new SiteInitializer().CreateAsync(newSite.SiteDirectory);
        Console.WriteLine($"Created site in {newSite.SiteDirectory}");
        return 0;
    }

    if (args[0] == "preview")
    {
        var preview = PreviewCommand.Parse(args);
        await PreviewServer.RunAsync(preview.Directory, preview.Port);
        return 0;
    }

    var command = SiteCommand.Parse(args);
    var hostBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = [],
        ContentRootPath = AppContext.BaseDirectory
    });
    hostBuilder.Services.AddControllersWithViews()
        .AddApplicationPart(typeof(RazorPageRenderer).Assembly);
    hostBuilder.Services.AddScoped<IPageRenderer, RazorPageRenderer>();

    // Build the service container without starting an HTTP listener.
    await using var host = hostBuilder.Build();
    await using var scope = host.Services.CreateAsyncScope();
    var pageRenderer = scope.ServiceProvider.GetRequiredService<IPageRenderer>();
    var markdownPipeline = MarkdownPipelineFactory.Create();
    var builder = new SiteBuilder(
        new ArticleParser(markdownPipeline),
        new MarkdownRenderer(markdownPipeline),
        pageRenderer,
        new StaticAssetCopier());
    var pageCount = await builder.BuildAsync(command.SiteDirectory, includeDrafts: command.Name == "dev");

    Console.WriteLine($"Built {pageCount} page(s) in {Path.Combine(command.SiteDirectory, "public")}");
    if (command.Name == "dev")
    {
        var liveReload = new LiveReload();
        using var watcher = new SiteWatcher(command.SiteDirectory, builder, liveReload);
        await PreviewServer.RunAsync(Path.Combine(command.SiteDirectory, "public"), command.Port, watcher, liveReload);
    }

    return 0;
}
catch (ArgumentException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 2;
}
catch (Exception exception) when (
    exception is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
{
    Console.Error.WriteLine($"{args[0]} failed: {exception.Message}");
    return 1;
}
