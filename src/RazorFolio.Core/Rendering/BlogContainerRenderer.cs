using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;

namespace RazorFolio.Core.Rendering;

public sealed class BlogContainerRenderer : HtmlCustomContainerRenderer
{
    protected override void Write(HtmlRenderer renderer, CustomContainer container)
    {
        var kind = container.Info;
        if (kind is not ("tip" or "warning" or "details") || !renderer.EnableHtmlForBlock)
        {
            base.Write(renderer, container);
            return;
        }

        var title = string.IsNullOrWhiteSpace(container.Arguments)
            ? kind switch { "tip" => "提示", "warning" => "注意", _ => "详情" }
            : container.Arguments;

        renderer.EnsureLine();
        if (kind == "details")
        {
            renderer.WriteLine("<details class=\"custom-container details\">");
            renderer.Write("<summary>").WriteEscape(title).WriteLine("</summary>");
            renderer.WriteChildren(container);
            renderer.WriteLine("</details>");
        }
        else
        {
            renderer.Write("<aside class=\"custom-container ").Write(kind).WriteLine("\">");
            renderer.Write("<p class=\"custom-container-title\">").WriteEscape(title).WriteLine("</p>");
            renderer.WriteChildren(container);
            renderer.WriteLine("</aside>");
        }
    }
}
