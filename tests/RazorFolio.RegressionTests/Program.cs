using System.Security.Cryptography;
using System.Xml.Linq;
using RazorFolio.Core;
using RazorFolio.Core.Assets;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Content;
using RazorFolio.Core.Diagnostics;
using RazorFolio.Core.Output;
using RazorFolio.Core.Rendering;
using RazorFolio.Themes.Default;

var runDirectory = Path.GetFullPath(Path.Combine("artifacts", "regression", Guid.NewGuid().ToString("N")));
Console.WriteLine($"Regression sites: {runDirectory}");

await BuildArticleSiteAsync(Path.Combine(runDirectory, "articles"));
Console.WriteLine("PASS: article URLs, build URL override, link diagnostics, drafts and metadata");
await RebuildSiteAsync(Path.Combine(runDirectory, "rebuild"));
Console.WriteLine("PASS: resource fingerprints and stale output cleanup");
await RejectConflictsAsync(Path.Combine(runDirectory, "conflicts"));
Console.WriteLine("PASS: output path conflicts");
await PreserveOutputOnWriteFailureAsync(Path.Combine(runDirectory, "write-failure"));
Console.WriteLine("PASS: failed writes preserve the previous file");
if (OperatingSystem.IsLinux())
{
    await RejectLinkedOutputAsync(Path.Combine(runDirectory, "linked-output"));
    Console.WriteLine("PASS: dangling output links are rejected");
}

static async Task BuildArticleSiteAsync(string site)
{
    Write(site, "razorfolio.json", """{"title":"Regression blog","url":"https://example.com/blog/"}""");
    Write(site, "content/notes/安装.md", """
        ---
        title: Setup
        date: 2025-01-01
        updated: 2025-02-01
        url: /guides/setup.html
        aliases: [/old.html]
        ---
        ## Install {#install}
        """);
    Write(site, "content/notes/links.md", """
        ---
        article: false
        ---
        [Source](%E5%AE%89%E8%A3%85.md#install)
        [Alias](../old.html#install)
        [Wrong case](../guides/Setup.html)
        [Missing anchor](%E5%AE%89%E8%A3%85.md#missing)
        [Draft](draft.md)
        """);
    Write(site, "content/notes/draft.md", "---\ndraft: true\n---\nDraft body.");

    var diagnostics = new List<ContentLinkDiagnostic>();
    var builder = CreateBuilder(diagnostics.Add);
    var production = SiteBuildTarget.Production(site);
    await builder.BuildAsync(production);
    var output = production.OutputDirectory;

    var links = Read(output, "notes/links.html");
    Require(links.Contains("href=\"../guides/setup.html#install\"", StringComparison.Ordinal),
        "The encoded Markdown link must resolve to the custom URL and keep its fragment.");
    Require(Read(output, "old.html").Contains("href=\"./guides/setup.html\"", StringComparison.Ordinal),
        "The legacy alias must link to the relocated article.");
    Require(!File.Exists(Path.Combine(output, "notes/安装.html")), "Only the custom article URL should be emitted.");
    Require(!File.Exists(Path.Combine(output, "notes/draft.html")), "The draft must be absent in production.");

    (string Code, string Link)[] expected =
    [
        ("RF1002", "../guides/Setup.html"),
        ("RF1005", "%E5%AE%89%E8%A3%85.md#missing"),
        ("RF1001", "draft.md")
    ];
    var actual = diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Link)).ToArray();
    Require(actual.Length == expected.Length && expected.All(actual.Contains),
        "Expected only case, missing anchor and unpublished draft warnings. Got: " + string.Join(", ", actual));

    var item = XDocument.Load(Path.Combine(output, "feed.xml")).Descendants("item").Single();
    const string canonical = "https://example.com/blog/guides/setup.html";
    Require((string?)item.Element("link") == canonical, "RSS must use the deployed custom URL.");
    Require((string?)item.Element("pubDate") == "Wed, 01 Jan 2025 00:00:00 GMT",
        "RSS must use the publication date.");
    XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    var entry = XDocument.Load(Path.Combine(output, "sitemap.xml")).Descendants(sitemapNamespace + "url")
        .Single(url => (string?)url.Element(sitemapNamespace + "loc") == canonical);
    Require((string?)entry.Element(sitemapNamespace + "lastmod") == "2025-02-01",
        "Sitemap must use the update date.");

    diagnostics.Clear();
    var development = SiteBuildTarget.Development(site);
    await builder.BuildAsync(development);
    Require(File.Exists(Path.Combine(development.OutputDirectory, "notes/draft.html")),
        "The development build must include drafts.");
    Require(!diagnostics.Any(diagnostic => diagnostic.Link == "draft.md"),
        "Development links to generated drafts must resolve.");

    var originalConfig = File.ReadAllBytes(Path.Combine(site, "razorfolio.json"));
    await builder.BuildAsync(SiteBuildTarget.Production(site, "https://publish.example.com/notes"));
    const string overriddenUrl = "https://publish.example.com/notes/guides/setup.html";
    Require(Read(output, "guides/setup.html").Contains($"rel=\"canonical\" href=\"{overriddenUrl}\"", StringComparison.Ordinal),
        "The build URL must replace the configured origin and path.");
    Require(Read(output, "404.html").Contains("href=\"/notes/assets/", StringComparison.Ordinal),
        "404 assets must use the overridden deployment path.");
    Require(Read(output, "feed.xml").Contains(overriddenUrl, StringComparison.Ordinal),
        "RSS links must use the build URL.");
    Require(originalConfig.SequenceEqual(File.ReadAllBytes(Path.Combine(site, "razorfolio.json"))),
        "The build URL must leave the site configuration untouched.");
}

static async Task RebuildSiteAsync(string site)
{
    Write(site, "content/post.md", "Original body.");
    Write(site, "content/removed.md", "Article to remove.");
    var builder = CreateBuilder();
    var target = SiteBuildTarget.Production(site);
    var output = target.OutputDirectory;

    await builder.BuildAsync(target);
    var removedArticle = Path.Combine(output, "removed.html");
    Require(File.Exists(removedArticle), "The initial build must emit the article that will be removed.");

    string SearchIndex() => Directory.GetFiles(output, "search-index.*.json").Single();
    var originalIndex = SearchIndex();
    var themeScript = Directory.GetFiles(Path.Combine(output, "assets"), "theme.*.js").Single();
    var diagramScript = Directory.GetFiles(Path.Combine(output, "assets"), "mermaid.*.js")
        .Single(path => !Path.GetFileName(path).StartsWith("mermaid.tiny.", StringComparison.Ordinal));
    var diagramLibrary = Directory.GetFiles(Path.Combine(output, "assets"), "mermaid.tiny.*.js").Single();
    Require(File.ReadAllText(diagramScript).Contains(Path.GetFileName(diagramLibrary), StringComparison.Ordinal),
        "The diagram loader must reference the fingerprinted library.");
    var diagramHash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(diagramScript)))[..16];
    Require(Path.GetFileName(diagramScript) == $"mermaid.{diagramHash}.js",
        "The loader fingerprint must match its final, rewritten bytes.");

    await builder.BuildAsync(target);
    Require(SearchIndex() == originalIndex, "An unchanged build must reuse the search index URL.");

    Write(output, "unmanaged.txt", "Keep this user file.");
    File.Delete(Path.Combine(site, "content", "removed.md"));
    Write(site, "content/post.md", "Revised body.");
    await builder.BuildAsync(target);

    var currentIndex = SearchIndex();
    Require(currentIndex != originalIndex, "A content change must produce a new search URL and remove the old index.");
    Require(Read(output, "post.html").Contains(Path.GetFileName(currentIndex), StringComparison.Ordinal),
        "The article must reference the current fingerprinted index.");
    Require(File.ReadAllBytes(currentIndex).SequenceEqual(File.ReadAllBytes(Path.Combine(output, "search-index.json"))),
        "The stable search endpoint must have the same contents as the fingerprinted index.");
    Require(File.Exists(themeScript), "An article edit must preserve the theme script URL.");
    Require(!File.Exists(removedArticle), "The removed article must be cleaned up.");
    Require(Read(output, "unmanaged.txt") == "Keep this user file.", "Cleanup must preserve untracked output.");
}

static async Task RejectConflictsAsync(string site)
{
    var builder = CreateBuilder();
    var target = SiteBuildTarget.Production(site);
    Write(site, "content/post.md", "---\naliases: [/notes.html]\n---\nPost.");
    Write(site, "content/notes.html/child.md", "Nested article.");
    await ExpectConflictAsync("both a file and a directory");

    File.Delete(Path.Combine(site, "content", "notes.html", "child.md"));
    Write(site, "static/notes.html", "Static file conflicts with an alias.");
    await ExpectConflictAsync("Static asset conflicts with generated output");
    Require(!File.Exists(Path.Combine(target.OutputDirectory, "notes.html")),
        "A conflicting static file must not be published.");

    async Task ExpectConflictAsync(string message)
    {
        try
        {
            await builder.BuildAsync(target);
        }
        catch (InvalidDataException exception) when (exception.Message.Contains(message, StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException($"Expected build to reject an output conflict: {message}.");
    }
}

static async Task PreserveOutputOnWriteFailureAsync(string site)
{
    const string existing = "Previous feed";
    Write(site, "feed.xml", existing);
    var feed = Path.Combine(site, "feed.xml");
    var invalidXml = new SiteConfiguration { Url = "https://example.com/", Title = "Invalid \u0001 title" };

    try
    {
        await RssWriter.WriteAsync(feed, invalidXml, []);
    }
    catch (ArgumentException)
    {
        Require(File.ReadAllText(feed) == existing, "A serialization failure must preserve the previous output.");
        Require(Directory.GetFiles(site).Length == 1, "A failed write must remove its temporary file.");
        return;
    }

    throw new InvalidOperationException("Expected RSS serialization to reject the illegal XML character.");
}

static async Task RejectLinkedOutputAsync(string site)
{
    Write(site, "content/post.md", "Post.");
    var target = SiteBuildTarget.Production(site);
    Directory.CreateDirectory(target.OutputDirectory);
    var link = Path.Combine(target.OutputDirectory, "index.html");
    var destination = Path.Combine(site, "outside-output.html");
    File.CreateSymbolicLink(link, destination);

    try
    {
        await CreateBuilder().BuildAsync(target);
    }
    catch (InvalidDataException exception) when (exception.Message.Contains("symbolic link", StringComparison.Ordinal))
    {
        Require(!File.Exists(destination), "A dangling output link must not create its target file.");
        Require(new FileInfo(link).LinkTarget == destination, "The build must leave the output link untouched.");
        return;
    }

    throw new InvalidOperationException("Expected the build to reject a dangling output link.");
}

static SiteBuilder CreateBuilder(Action<ContentLinkDiagnostic>? reportDiagnostic = null)
{
    var pipeline = MarkdownPipelineFactory.Create();
    return new SiteBuilder(new ArticleParser(pipeline), new MarkdownRenderer(pipeline),
        new RazorPageRenderer(), new StaticAssetCopier(), DefaultThemeAssets.All, reportDiagnostic);
}

static string Read(string root, string path) => File.ReadAllText(Path.Combine(root, path));

static void Write(string root, string path, string content)
{
    var file = Path.Combine(root, path);
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    File.WriteAllText(file, content);
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
