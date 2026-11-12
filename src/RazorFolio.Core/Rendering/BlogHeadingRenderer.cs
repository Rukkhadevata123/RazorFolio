using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace RazorFolio.Core.Rendering;

public sealed class BlogHeadingRenderer : HeadingRenderer
{
    protected override void Write(HtmlRenderer renderer, HeadingBlock heading)
    {
        var id = heading.TryGetAttributes()?.Id;
        if (!renderer.EnableHtmlForBlock || heading.Level is < 2 or > 6 || string.IsNullOrEmpty(id))
        {
            base.Write(renderer, heading);
            return;
        }

        var tag = $"h{heading.Level}";
        renderer.Write('<').Write(tag).WriteAttributes(heading).Write('>');
        renderer.WriteLeafInline(heading);
        renderer.Write(" <a class=\"heading-anchor\" href=\"")
            .WriteEscapeUrl("#" + Uri.EscapeDataString(id))
            .Write("\" aria-label=\"链接到本节\">#</a>");
        renderer.Write("</").Write(tag).WriteLine('>');
    }
}
