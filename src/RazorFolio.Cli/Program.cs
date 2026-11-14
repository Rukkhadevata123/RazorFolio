using System.Reflection;
using RazorFolio.Cli.Commands;
using RazorFolio.Cli.Hosting;
using RazorFolio.Core;
using RazorFolio.Core.Assets;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Content;
using RazorFolio.Core.Rendering;
using RazorFolio.Themes.Default;

if (args is [] or ["--help" or "-h"])
{
    Console.WriteLine("""
        RazorFolio (rf)

        Usage:
          rf new <directory>
          rf build --site <directory>
          rf dev --site <directory> [--port 8080]
          rf preview --site <directory> [--directory public] [--port 8080]
          rf --version

        Convert content/**/*.md files to public/**/*.html, preserving subdirectories.
        Render complete HTML pages using the precompiled default Razor theme.

        Preview serves existing files on 127.0.0.1 without rebuilding them.

        Dev builds into .razorfolio/dev/ with drafts and reloads browsers on source changes.

        Build and dev warn about missing local Markdown link targets without failing the build.

        New creates a minimal site in a new or empty directory.
        """);

    return 0;
}

if (args is ["--version"])
{
    var assembly = Assembly.GetExecutingAssembly();
    var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
    Console.WriteLine($"RazorFolio {version}");
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
        var site = await SiteConfigurationLoader.LoadAsync(preview.SiteDirectory);
        var pathBase = site.Url is null ? "" : new Uri(site.Url).AbsolutePath.TrimEnd('/');
        await PreviewServer.RunAsync(preview.Directory, preview.Port, pathBase: pathBase);
        return 0;
    }

    var command = SiteCommand.Parse(args);
    var pageRenderer = new RazorPageRenderer();
    var markdownPipeline = MarkdownPipelineFactory.Create();
    var builder = new SiteBuilder(
        new ArticleParser(markdownPipeline),
        new MarkdownRenderer(markdownPipeline),
        pageRenderer,
        new StaticAssetCopier(),
        DefaultThemeAssets.All,
        diagnostic => Console.Error.WriteLine(
            $"{diagnostic.SourcePath}({diagnostic.Line},{diagnostic.Column}): warning {diagnostic.Code}: "
            + $"{diagnostic.Message} Link: {diagnostic.Link}"));
    var target = command.Name == "dev"
        ? SiteBuildTarget.Development(command.SiteDirectory)
        : SiteBuildTarget.Production(command.SiteDirectory);
    var pageCount = await builder.BuildAsync(target);

    Console.WriteLine($"Built {pageCount} page(s) in {target.OutputDirectory}");
    if (command.Name == "dev")
    {
        var liveReload = new LiveReload();
        using var watcher = new SiteWatcher(target, builder, liveReload);
        await PreviewServer.RunAsync(target.OutputDirectory, command.Port, watcher, liveReload);
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
