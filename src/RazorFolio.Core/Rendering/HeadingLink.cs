using System.Text;
using Markdig.Extensions.Mathematics;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace RazorFolio.Core.Rendering;

public sealed record HeadingLink(int Level, string Text, string Url)
{
    public static HeadingLink FromHeading(HeadingBlock heading)
    {
        var text = new StringBuilder();
        Append(heading.Inline);
        return new HeadingLink(heading.Level, text.ToString(), "#" + Uri.EscapeDataString(heading.GetAttributes().Id!));

        void Append(Inline? inline)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(literal.Content);
                    break;
                case CodeInline code:
                    text.Append(code.Content);
                    break;
                case MathInline math:
                    text.Append(math.Content);
                    break;
                case HtmlEntityInline entity:
                    text.Append(entity.Transcoded);
                    break;
                case AutolinkInline link:
                    text.Append(link.Url);
                    break;
                case LineBreakInline:
                    text.Append(' ');
                    break;
                case ContainerInline container:
                    foreach (var child in container)
                    {
                        Append(child);
                    }
                    break;
            }
        }
    }
}
