using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace RazorFolio.Cli.Hosting;

internal static class PreviewServer
{
    public static async Task RunAsync(string directory, int port)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Preview directory not found: {directory}. Run rf build first.");
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = [],
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));

        using var files = new PhysicalFileProvider(directory);
        await using var app = builder.Build();
        app.UseFileServer(new FileServerOptions
        {
            FileProvider = files,
            EnableDirectoryBrowsing = false
        });

        Console.WriteLine($"Previewing {directory} at http://127.0.0.1:{port}. Press Ctrl+C to stop.");
        await app.RunAsync();
    }
}
