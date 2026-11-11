using Markdig;
using Markdig.Syntax;

namespace RazorFolio.Core.Rendering;

public sealed class MarkdownRenderer
{
    public string Render(MarkdownDocument document)
    {
        return Markdown.ToHtml(document);
    }
}
