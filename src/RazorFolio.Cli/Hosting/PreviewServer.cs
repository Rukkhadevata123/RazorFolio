using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RazorFolio.Cli.Hosting;

internal static class PreviewServer
{
    public static async Task RunAsync(
        string directory, int port, IHostedService? watcher = null, LiveReload? liveReload = null)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Preview directory not found: {directory}. Run rf build first.");
        }

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));
        if (watcher is not null)
        {
            builder.Services.AddSingleton(watcher);
        }

        using var files = new PhysicalFileProvider(directory);
        await using var app = builder.Build();
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".txt"] = "text/plain; charset=utf-8";

        if (liveReload is not null)
        {
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            liveReload.Configure(app, files);
        }

        app.UseFileServer(new FileServerOptions
        {
            FileProvider = files,
            EnableDirectoryBrowsing = false,
            StaticFileOptions =
            {
                ContentTypeProvider = contentTypes,
                OnPrepareResponse = context =>
                {
                    if (liveReload is not null)
                    {
                        context.Context.Response.Headers.CacheControl = "no-store";
                    }
                }
            }
        });

        Console.WriteLine($"Previewing {directory} at http://127.0.0.1:{port}. Press Ctrl+C to stop.");
        await app.RunAsync();
    }
}
