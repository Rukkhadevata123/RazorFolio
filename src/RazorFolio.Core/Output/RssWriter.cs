using System.Net;
using System.Xml.Linq;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Rendering;

namespace RazorFolio.Core.Output;

public static class RssWriter
{
    private const int MaxItems = 20;

    public static async Task WriteAsync(
        string outputPath,
        SiteConfiguration site,
        IReadOnlyList<ArticleLink> orderedArticles,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var siteUrl = site.Url ?? throw new InvalidDataException("A site URL is required to generate RSS.");
        var channel = new XElement("channel",
            new XElement("title", site.Title),
            new XElement("link", siteUrl),
            new XElement("description", site.Description ?? site.Title),
            new XElement("language", site.Language),
            orderedArticles.Take(MaxItems).Select(article => CreateItem(article, siteUrl)));
        var document = new XDocument(new XElement("rss", new XAttribute("version", "2.0"), channel));

        cancellationToken.ThrowIfCancellationRequested();
        await using var target = File.Create(outputPath);
        await document.SaveAsync(target, SaveOptions.None, cancellationToken);
    }

    private static XElement CreateItem(ArticleLink article, string siteUrl)
    {
        var url = PageUrl.GetAbsolute(article.Url, siteUrl)!;
        var item = new XElement("item",
            new XElement("title", article.Info.Title),
            new XElement("link", url),
            new XElement("guid", new XAttribute("isPermaLink", "true"), url));
        if (article.Info.Description is { } description)
        {
            // RSS descriptions may be interpreted as HTML; preserve the author's plain text.
            item.Add(new XElement("description", WebUtility.HtmlEncode(description)));
        }

        return item;
    }
}
