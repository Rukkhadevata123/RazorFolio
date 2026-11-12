using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace RazorFolio.Core.Content;

public sealed class ExplicitHeadingIdsExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        // Register before AutoIdentifiers so explicit IDs are set before automatic generation.
        pipeline.BlockParsers.Find<HeadingBlockParser>()!.Closed += OnHeadingClosed;
        pipeline.BlockParsers.FindExact<ParagraphBlockParser>()!.Closed += OnHeadingClosed;
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) { }

    private static void OnHeadingClosed(BlockProcessor processor, Block block)
    {
        if (block is not HeadingBlock heading) return;

        // Inspect source escapes before inline parsing merges escaped braces into literal text.
        var text = heading.Lines.ToString().TrimEnd();
        var marker = text.LastIndexOf("{#", StringComparison.Ordinal);
        if (text.EndsWith('}') && marker >= 0
            && (marker == 0 || char.IsWhiteSpace(text[marker - 1]))
            && !text[marker..].Contains('\\'))
        {
            heading.ProcessInlinesEnd += ApplyExplicitId;
        }
    }

    private static void ApplyExplicitId(InlineProcessor processor, Inline? inline)
    {
        var heading = (HeadingBlock)processor.Block!;
        if (heading.Inline?.LastChild is not LiteralInline literal) return;

        var text = literal.Content.ToString().TrimEnd();
        var marker = text.LastIndexOf("{#", StringComparison.Ordinal);
        if (!text.EndsWith('}') || marker < 0
            || (marker == 0 && (literal.IsFirstCharacterEscaped || literal.PreviousSibling is not null))
            || (marker > 0 && !char.IsWhiteSpace(text[marker - 1])))
        {
            return;
        }

        heading.GetAttributes().Id = text[(marker + 2)..^1];
        heading.SetData(typeof(ExplicitHeadingIdsExtension), true);
        var remaining = text[..marker].TrimEnd();
        if (remaining.Length == 0)
        {
            literal.Remove();
        }
        else
        {
            literal.Content = new StringSlice(remaining);
        }
    }

    public static void Validate(MarkdownDocument document, string sourcePath)
    {
        var linesById = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var id = heading.GetAttributes().Id;
            var line = heading.Line + 1;
            if (heading.GetData(typeof(ExplicitHeadingIdsExtension)) is true
                && (string.IsNullOrEmpty(id) || id.Any(character =>
                    !char.IsLetterOrDigit(character) && character is not ('-' or '_' or '.' or ':'))))
            {
                throw new InvalidDataException(
                    $"Invalid heading ID '{id}' in '{sourcePath}' at line {line}. Use letters, digits, '-', '_', '.' or ':'.");
            }

            if (id is null) continue;

            if (!linesById.TryAdd(id, line))
            {
                throw new InvalidDataException(
                    $"Duplicate heading ID '{id}' in '{sourcePath}' at lines {linesById[id]} and {line}.");
            }
        }
    }
}
