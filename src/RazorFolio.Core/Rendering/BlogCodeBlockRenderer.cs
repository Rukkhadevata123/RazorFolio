using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace RazorFolio.Core.Rendering;

public sealed class BlogCodeBlockRenderer : CodeBlockRenderer
{
    protected override void Write(HtmlRenderer renderer, CodeBlock block)
    {
        if (!renderer.EnableHtmlForBlock)
        {
            base.Write(renderer, block);
            return;
        }

        var language = (block as FencedCodeBlock)?.Info ?? "";
        renderer.EnsureLine();
        renderer.Write("<div class=\"code-block\" data-language=\"").WriteEscape(language).WriteLine("\">");
        renderer.Write("<div class=\"code-toolbar\"><span class=\"code-language\">")
            .WriteEscape(string.IsNullOrEmpty(language) ? "纯文本" : language)
            .WriteLine("</span></div>");
        base.Write(renderer, block);
        renderer.WriteLine("</div>");
    }
}
