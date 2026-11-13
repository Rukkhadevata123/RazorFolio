using System.Text;
using Markdig.Extensions.Mathematics;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace RazorFolio.Core.Content;

public static class MarkdownTextExtractor
{
    public static string Extract(MarkdownDocument document)
    {
        var text = new StringBuilder();
        AppendBlock(document, text);
        return CollapseWhitespace(text);
    }

    private static void AppendBlock(Block block, StringBuilder text)
    {
        switch (block)
        {
            // Extracting text from raw HTML blocks requires an HTML parser.
            case HtmlBlock:
                break;
            case CodeBlock code:
                text.Append(code.Lines.ToString());
                break;
            case LeafBlock leaf:
                AppendInline(leaf.Inline, text);
                break;
            case ContainerBlock container:
                foreach (var child in container)
                {
                    AppendBlock(child, text);
                }
                break;
        }

        text.Append(' ');
    }

    private static void AppendInline(Inline? inline, StringBuilder text)
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
                    AppendInline(child, text);
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
