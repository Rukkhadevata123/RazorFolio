using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Syntax;

namespace RazorFolio.Core.Rendering;

public sealed class MarkdownRenderer(MarkdownPipeline pipeline)
{
    public string Render(MarkdownDocument document, Func<string, string> linkRewriter)
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
        renderer.Render(document);
        return writer.ToString();
    }
}
