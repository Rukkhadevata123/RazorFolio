using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RazorFolio.Cli.Commands;
using RazorFolio.Core;
using RazorFolio.Core.Content;
using RazorFolio.Core.Rendering;
using RazorFolio.Themes.Default;

if (args.Length == 0 || (args.Length == 1 && args[0] is "--help" or "-h"))
{
    Console.WriteLine("""
        RazorFolio (rf)

        Usage:
          rf build --site <directory>

        Convert top-level content/*.md files to public/*.html.
        Render complete HTML pages using the precompiled default Razor theme.

        Planned commands: new, dev, preview.
        """);

    return 0;
}

try
{
    var command = BuildCommand.Parse(args);
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
    var builder = new SiteBuilder(new ArticleParser(), new MarkdownRenderer(), pageRenderer);
    var pageCount = await builder.BuildAsync(command.SiteDirectory);

    Console.WriteLine($"Built {pageCount} page(s) in {Path.Combine(command.SiteDirectory, "public")}");
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
    Console.Error.WriteLine($"Build failed: {exception.Message}");
    return 1;
}
