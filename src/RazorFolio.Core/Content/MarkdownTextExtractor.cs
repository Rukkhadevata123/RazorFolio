using System.Text;
using Markdig.Extensions.Mathematics;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace RazorFolio.Core.Content;

public static class MarkdownTextExtractor
{
    public static string Extract(MarkdownDocument document)
        => Extract(document, forReadingTime: false);

    internal static string ExtractForReadingTime(MarkdownDocument document)
        => Extract(document, forReadingTime: true);

    private static string Extract(MarkdownDocument document, bool forReadingTime)
    {
        var text = new StringBuilder();
        AppendBlock(document, text, forReadingTime);
        return CollapseWhitespace(text);
    }

    private static void AppendBlock(Block block, StringBuilder text, bool forReadingTime)
    {
        switch (block)
        {
            // Extracting text from raw HTML blocks requires an HTML parser.
            case HtmlBlock:
                break;
            case MathBlock when forReadingTime:
                break;
            case FencedCodeBlock fenced when forReadingTime &&
                string.Equals(fenced.Info, "mermaid", StringComparison.OrdinalIgnoreCase):
                break;
            case CodeBlock code:
                text.Append(code.Lines.ToString());
                break;
            case LeafBlock leaf:
                AppendInline(leaf.Inline, text, forReadingTime);
                break;
            case ContainerBlock container:
                foreach (var child in container)
                {
                    AppendBlock(child, text, forReadingTime);
                }
                break;
        }

        text.Append(' ');
    }

    private static void AppendInline(Inline? inline, StringBuilder text, bool forReadingTime)
    {
        switch (inline)
        {
            case LiteralInline literal:
                text.Append(literal.Content);
                break;
            case CodeInline code:
                text.Append(code.Content);
                break;
            case MathInline math when !forReadingTime:
                text.Append(math.Content);
                break;
            case HtmlEntityInline entity:
                text.Append(entity.Transcoded);
                break;
            case AutolinkInline link when !forReadingTime:
                text.Append(link.Url);
                break;
            case LineBreakInline:
                text.Append(' ');
                break;
            case LinkInline { IsImage: true } when forReadingTime:
                break;
            case ContainerInline container:
                foreach (var child in container)
                {
                    AppendInline(child, text, forReadingTime);
                }
                break;
        }
    }

    private static string CollapseWhitespace(StringBuilder source)
    {
        var result = new StringBuilder(source.Length);
        var pendingSpace = false;
        for (var index = 0; index < source.Length; index++)
        {
            var character = source[index];
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = result.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                result.Append(' ');
                pendingSpace = false;
            }

            result.Append(character);
        }

        return result.ToString();
    }
}
