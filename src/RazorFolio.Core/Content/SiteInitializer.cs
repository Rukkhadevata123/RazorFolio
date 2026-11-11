using System.Text.Json;
using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Content;

public sealed class SiteInitializer
{
    public async Task CreateAsync(string siteDirectory, CancellationToken cancellationToken = default)
    {
        var siteRoot = Path.GetFullPath(siteDirectory);
        if (Directory.Exists(siteRoot) && Directory.EnumerateFileSystemEntries(siteRoot).Any())
        {
            throw new IOException($"The site directory must be empty: {siteRoot}");
        }

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.Combine(siteRoot, "content"));
        Directory.CreateDirectory(Path.Combine(siteRoot, "static"));

        var configuration = JsonSerializer.Serialize(new SiteConfiguration(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        await WriteNewFileAsync(Path.Combine(siteRoot, "razorfolio.json"), configuration + "\n", cancellationToken);
        await WriteNewFileAsync(Path.Combine(siteRoot, ".gitignore"), "/public/\n/.razorfolio/\n", cancellationToken);
        await WriteNewFileAsync(Path.Combine(siteRoot, "content", "hello.md"), """
            ---
            title: 你好，RazorFolio
            ---

            # 你好，RazorFolio

            这是你的第一篇文章。编辑这个文件，然后使用 `rf dev --site .` 查看效果。

            - 文章放在 `content/`。
            - 图片和附件放在 `static/`。
            - 站点名称在 `razorfolio.json` 中设置。

            """, cancellationToken);
    }

    private static async Task WriteNewFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        // Never overwrite a file, even if another process creates it after the directory check.
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }
}
