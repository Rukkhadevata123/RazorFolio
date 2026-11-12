using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace RazorFolio.Core.Rendering;

public sealed class MarkdownRenderer(MarkdownPipeline pipeline)
{
    public RenderedArticle Render(MarkdownDocument document, Func<string, string> linkRewriter)
    {
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        renderer.LinkRewriter = linkRewriter;

        var containerRenderer = renderer.ObjectRenderers.FindExact<HtmlCustomContainerRenderer>();
        if (containerRenderer is not null)
        {
            renderer.ObjectRenderers.Remove(containerRenderer);
        }

        renderer.ObjectRenderers.Insert(0, new BlogContainerRenderer());
        renderer.ObjectRenderers.Insert(0, new BlogHeadingRenderer());
        renderer.ObjectRenderers.Insert(0, new BlogCodeBlockRenderer());
        var title = document.FirstOrDefault() as HeadingBlock;
        string? titleHtml = null;
        if (title?.Level == 1)
        {
            renderer.Render(title);
            titleHtml = writer.ToString();
            writer.GetStringBuilder().Clear();
        }
        foreach (var block in document)
        {
            if (titleHtml is null || block != title) renderer.Render(block);
        }
        HeadingLink[] headings =
        [
            .. document.Descendants<HeadingBlock>()
                .Where(heading => heading.Level is >= 2 and <= 6 && heading.GetAttributes().Id is not null)
                .Select(HeadingLink.FromHeading)
        ];
        return new RenderedArticle(writer.ToString(), titleHtml, headings);
    }
}
