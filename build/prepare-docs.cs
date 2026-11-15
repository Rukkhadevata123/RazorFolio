using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var repo = Path.GetFullPath("..", Path.GetDirectoryName(ScriptPath())!);
var output = "artifacts/docs-site";
var revision = "main";
var seen = new HashSet<string>(StringComparer.Ordinal);
const string usage = "Usage: dotnet run --file build/prepare-docs.cs -- [--output <directory>] [--revision <ref>]";

try
{
    for (var index = 0; index < args.Length; index += 2)
    {
        if (index + 1 >= args.Length || !seen.Add(args[index]) || string.IsNullOrWhiteSpace(args[index + 1]))
        {
            throw new ArgumentException(usage);
        }
        switch (args[index])
        {
            case "--output":
                output = args[index + 1];
                break;
            case "--revision":
                revision = args[index + 1];
                break;
            default:
                throw new ArgumentException(usage);
        }
    }

    var site = Path.GetFullPath(output);
    if (File.Exists(site) || (Directory.Exists(site) && Directory.EnumerateFileSystemEntries(site).Any()))
    {
        throw new ArgumentException($"Choose a new or empty output directory: {site}");
    }

    Guide[] guides =
    [
        new("README.md", "guide.md", "开始使用", ["入门", "开发"]),
        new("docs/native-package.md", "docs/native-package.md", "开始使用", ["入门", "部署"]),
        new("docs/configuration.md", "docs/configuration.md", "使用指南", ["配置", "部署"]),
        new("docs/writing.md", "docs/writing.md", "使用指南", ["写作", "配置"]),
        new("docs/github-pages.md", "docs/github-pages.md", "使用指南", ["部署"]),
        new("docs/releasing.md", "docs/releasing.md", "开发者", ["开发", "部署"])
    ];
    var destinations = guides.ToDictionary(guide => guide.Source, guide => guide.Destination, StringComparer.Ordinal);
    var json = new MetadataJson(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    string Quote(string value) => JsonSerializer.Serialize(value, json.String);

    // Prepare every page before writing the site.
    var pages = new List<(string Destination, string Content)>();
    foreach (var guide in guides)
    {
        var text = File.ReadAllText(Path.Combine(repo, guide.Source)).ReplaceLineEndings("\n");
        var title = Regex.Match(text, "^# (.+)");
        if (!title.Success)
        {
            throw new InvalidDataException($"Expected an opening H1 in {guide.Source}");
        }
        var metadata = $"---\ntitle: {Quote(title.Groups[1].Value)}\n"
            + $"category: [{Quote(guide.Category)}]\n"
            + $"tags: [{string.Join(", ", guide.Tags.Select(Quote))}]\n---\n\n";
        pages.Add((guide.Destination, metadata + PrepareMarkdown(text, guide, repo, revision, destinations)));
    }

    Directory.CreateDirectory(site);
    File.Copy(Path.Combine(repo, "docs-site/razorfolio.json"), Path.Combine(site, "razorfolio.json"));
    foreach (var page in pages)
    {
        var path = Path.Combine(site, "content", page.Destination);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, page.Content);
    }

    var downloads = Path.Combine(site, "static/docs/workflows");
    Directory.CreateDirectory(downloads);
    foreach (var workflow in Directory.EnumerateFiles(Path.Combine(repo, "docs/workflows"), "*.yml"))
    {
        // Plain text downloads also work with the local preview server's MIME types.
        File.Copy(workflow, Path.Combine(downloads, Path.GetFileName(workflow) + ".txt"));
    }
    Console.WriteLine($"Prepared {pages.Count} guides in {site}");
    return 0;
}
catch (Exception exception) when (exception is ArgumentException or IOException
    or InvalidDataException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static string PrepareMarkdown(
    string text, Guide guide, string repo, string revision, IReadOnlyDictionary<string, string> destinations)
{
    var result = new StringBuilder();
    string? fence = null;
    var headingIds = new HashSet<string>(StringComparer.Ordinal);
    using var reader = new StringReader(text);
    while (reader.ReadLine() is { } sourceLine)
    {
        var line = sourceLine;
        var marker = Regex.Match(line, @"^\s*(`{3,}|~{3,})");
        if (marker.Success)
        {
            var value = marker.Groups[1].Value;
            if (fence is null)
            {
                fence = value;
            }
            else if (value[0] == fence[0] && value.Length >= fence.Length)
            {
                fence = null;
            }
        }
        else if (fence is null)
        {
            // Keep GitHub-style heading links working with the SSG's explicit heading IDs.
            var heading = Regex.Match(line, @"^(#{1,6}) (.+?)\s*$");
            if (heading.Success && !Regex.IsMatch(heading.Groups[2].Value, @"\{#[^}]+\}$"))
            {
                var slug = Regex.Replace(heading.Groups[2].Value.ToLowerInvariant(), @"[^\w\- ]", "").Replace(' ', '-');
                var identifier = slug;
                var suffix = 1;
                while (!headingIds.Add(identifier))
                {
                    identifier = $"{slug}-{suffix++}";
                }
                line = line.TrimEnd() + $" {{#{identifier}}}";
            }

            // Repository guides use inline Markdown links. Keep inline code examples literal.
            var pieces = Regex.Split(line, @"(`+[^`]*`+)");
            for (var index = 0; index < pieces.Length; index += 2)
            {
                pieces[index] = Regex.Replace(pieces[index], @"(!?\[[^\]\n]+\])\(([^\s)]+)\)", match =>
                    RewriteLink(match, guide, repo, revision, destinations));
            }
            line = string.Concat(pieces);
        }
        result.Append(line).Append('\n');
    }
    if (fence is not null)
    {
        throw new InvalidDataException($"Unclosed code fence in {guide.Source}");
    }
    return result.ToString();
}

static string RewriteLink(
    Match match, Guide guide, string repo, string revision, IReadOnlyDictionary<string, string> destinations)
{
    var url = match.Groups[2].Value;
    if (url.StartsWith("//", StringComparison.Ordinal) || Uri.TryCreate(url, UriKind.Absolute, out _))
    {
        return match.Value;
    }
    var suffixIndex = url.IndexOfAny(['?', '#']);
    var path = suffixIndex < 0 ? url : url[..suffixIndex];
    var suffix = suffixIndex < 0 ? "" : url[suffixIndex..];
    if (path.Length == 0) return match.Value;

    var sourceDirectory = Path.GetDirectoryName(Path.Combine(repo, guide.Source))!;
    var target = Path.GetFullPath(Uri.UnescapeDataString(path), sourceDirectory);
    var relative = Path.GetRelativePath(repo, target).Replace(Path.DirectorySeparatorChar, '/');
    if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith("../", StringComparison.Ordinal)
        || (!File.Exists(target) && !Directory.Exists(target)))
    {
        throw new InvalidDataException($"Missing or out-of-repository documentation target in {guide.Source}: {url}");
    }

    string link;
    if (destinations.TryGetValue(relative, out var destination)
        || relative.StartsWith("docs/workflows/", StringComparison.Ordinal))
    {
        destination ??= relative + ".txt";
        var fromDirectory = Path.GetDirectoryName(Path.Combine(repo, guide.Destination))!;
        link = EncodePath(Path.GetRelativePath(fromDirectory, Path.Combine(repo, destination))) + suffix;
    }
    else
    {
        var kind = Directory.Exists(target) ? "tree" : "blob";
        link = $"https://github.com/Rukkhadevata123/RazorFolio/{kind}/{Uri.EscapeDataString(revision)}/"
            + EncodePath(relative) + suffix;
    }
    return $"{match.Groups[1].Value}({link})";
}

static string EncodePath(string path) =>
    string.Join("/", path.Replace(Path.DirectorySeparatorChar, '/').Split('/').Select(Uri.EscapeDataString));

static string ScriptPath([CallerFilePath] string path = "") => path;

sealed record Guide(string Source, string Destination, string Category, string[] Tags);

[JsonSerializable(typeof(string))]
partial class MetadataJson : JsonSerializerContext;
