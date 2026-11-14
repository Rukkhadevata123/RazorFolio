using System.Net;
using System.Text.Json;
using RazorFolio.Core.Serialization;

namespace RazorFolio.Core.Output;

public static class RedirectWriter
{
    public static async Task WriteAsync(
        string outputPath, string targetUrl, string? canonicalUrl, string language,
        CancellationToken cancellationToken = default)
    {
        var encodedTarget = WebUtility.HtmlEncode(targetUrl);
        var canonical = canonicalUrl is null
            ? ""
            : $"<link rel=\"canonical\" href=\"{WebUtility.HtmlEncode(canonicalUrl)}\">";
        var html = $$"""
            <!doctype html>
            <html lang="{{WebUtility.HtmlEncode(language)}}">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="robots" content="noindex">
                {{canonical}}
                <title>页面已迁移</title>
                <noscript><meta http-equiv="refresh" content="0;url={{encodedTarget}}"></noscript>
            </head>
            <body>
                <p>页面已迁移，<a href="{{encodedTarget}}">前往新地址</a>。</p>
                <script>
                (() => {
                    const target = new URL({{JsonSerializer.Serialize(targetUrl, RazorFolioJsonContext.Default.String)}}, window.location.href);
                    target.search = window.location.search;
                    target.hash = window.location.hash;
                    window.location.replace(target.href);
                })();
                </script>
            </body>
            </html>
            """;

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, html, cancellationToken);
    }
}
