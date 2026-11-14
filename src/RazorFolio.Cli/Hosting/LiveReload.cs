using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace RazorFolio.Cli.Hosting;

internal sealed class LiveReload
{
    private string revision = Guid.NewGuid().ToString("N");

    public void Publish() => Volatile.Write(ref revision, Guid.NewGuid().ToString("N"));

    public void Configure(WebApplication app, IFileProvider files)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/_rf/events")
            {
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    context.RequestAborted, app.Lifetime.ApplicationStopping);
                await SendEventsAsync(context, cancellation.Token);
                return;
            }

            var path = context.Request.Path.Value ?? "";
            if (!HttpMethods.IsGet(context.Request.Method)
                || !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var file = files.GetFileInfo(path);
            if (!file.Exists || file.IsDirectory)
            {
                await next(context);
                return;
            }

            await WritePageAsync(context, file);
        });
    }

    public async Task WritePageAsync(HttpContext context, IFileInfo file)
    {
        var pageRevision = Volatile.Read(ref revision);
        using var reader = new StreamReader(file.CreateReadStream());
        var html = await reader.ReadToEndAsync(context.RequestAborted);
        var script = $$"""
                <script>
                (() => {
                    const revision = "{{pageRevision}}";
                    const events = new EventSource("/_rf/events");
                    events.onmessage = event => {
                        if (event.data !== revision) {
                            events.close();
                            location.reload();
                        }
                    };
                })();
                </script>
                """;
        var bodyEnd = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
        html = bodyEnd >= 0 ? html.Insert(bodyEnd, script) : html + script;

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private async Task SendEventsAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-store";
        string? sentRevision = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var current = Volatile.Read(ref revision);
                if (current != sentRevision)
                {
                    await context.Response.WriteAsync($"data: {current}\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                    sentRevision = current;
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser disconnected or the development server is stopping.
        }
    }
}
