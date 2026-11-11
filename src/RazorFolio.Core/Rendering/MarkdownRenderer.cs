using Markdig;
using Markdig.Syntax;

namespace RazorFolio.Core.Rendering;

public sealed class MarkdownRenderer(MarkdownPipeline pipeline)
{
    public string Render(MarkdownDocument document)
    {
        return Markdown.ToHtml(document, pipeline);
    }
}
