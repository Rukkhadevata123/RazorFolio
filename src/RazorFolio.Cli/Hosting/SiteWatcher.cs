using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using RazorFolio.Core;

namespace RazorFolio.Cli.Hosting;

internal sealed class SiteWatcher(SiteBuildTarget target, SiteBuilder builder, LiveReload liveReload) : BackgroundService
{
    private readonly Channel<bool> changes = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true
    });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var watcher = new FileSystemWatcher(target.SiteDirectory);
        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += (_, args) =>
        {
            QueueChange(args.OldFullPath);
            QueueChange(args.FullPath);
        };
        watcher.Error += (_, args) =>
        {
            Console.Error.WriteLine($"File watcher error: {args.GetException().Message}");
            changes.Writer.TryWrite(true);
        };
        watcher.EnableRaisingEvents = true;

        // Catch changes made between the initial build and watcher startup.
        changes.Writer.TryWrite(true);
        Console.WriteLine("Watching content/, static/ and razorfolio.json. Browser reload is enabled.");

        try
        {
            while (await changes.Reader.WaitToReadAsync(stoppingToken))
            {
                // Coalesce editor save events and serialize all rebuilds.
                await Task.Delay(200, stoppingToken);
                while (changes.Reader.TryRead(out _)) { }

                try
                {
                    var count = await builder.BuildAsync(target, stoppingToken);
                    liveReload.Publish();
                    Console.WriteLine($"Rebuilt {count} page(s). Notified browsers.");
                }
                catch (Exception exception) when (
                    exception is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
                {
                    Console.Error.WriteLine($"Rebuild failed: {exception.Message}");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown; FileSystemWatcher is disposed by the using statement.
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs args) => QueueChange(args.FullPath);

    private void QueueChange(string path)
    {
        var relative = Path.GetRelativePath(target.SiteDirectory, path);
        var root = relative.Split(Path.DirectorySeparatorChar)[0];
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (string.Equals(root, "content", comparison) || string.Equals(root, "static", comparison)
            || string.Equals(relative, "razorfolio.json", comparison))
        {
            changes.Writer.TryWrite(true);
        }
    }
}
