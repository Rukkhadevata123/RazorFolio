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
        // The root watcher notices configuration changes and source directories being recreated.
        // Recursive watchers cover source trees only, never our own generated output.
        using var rootWatcher = CreateWatcher(target.SiteDirectory, includeSubdirectories: false);
        List<FileSystemWatcher> sourceWatchers = [];

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
                    RefreshSourceWatchers();
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
        finally
        {
            foreach (var watcher in sourceWatchers) watcher.Dispose();
        }

        void RefreshSourceWatchers()
        {
            List<FileSystemWatcher> replacements = [];
            try
            {
                foreach (var name in new[] { "content", "static" })
                {
                    var path = Path.Combine(target.SiteDirectory, name);
                    if (Directory.Exists(path)) replacements.Add(CreateWatcher(path, includeSubdirectories: true));
                }
            }
            catch
            {
                foreach (var watcher in replacements) watcher.Dispose();
                throw;
            }

            // Enable replacements first so edits during a rebuild aren't lost between subscriptions.
            foreach (var watcher in sourceWatchers) watcher.Dispose();
            sourceWatchers = replacements;
        }
    }

    private FileSystemWatcher CreateWatcher(string directory, bool includeSubdirectories)
    {
        var watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };
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
        try
        {
            watcher.EnableRaisingEvents = true;
            return watcher;
        }
        catch
        {
            watcher.Dispose();
            throw;
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
