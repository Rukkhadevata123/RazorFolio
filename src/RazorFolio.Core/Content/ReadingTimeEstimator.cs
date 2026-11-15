using System.Globalization;
using System.Text;
using Markdig.Syntax;

namespace RazorFolio.Core.Content;

internal static class ReadingTimeEstimator
{
    private const int CharactersPerMinute = 300;
    private const int WordsPerMinute = 200;

    public static int EstimateMinutes(MarkdownDocument document)
    {
        var text = MarkdownTextExtractor.ExtractForReadingTime(document);
        var characters = 0;
        var words = 0;
        var insideWord = false;

        foreach (var rune in text.EnumerateRunes())
        {
            if (IsEastAsianCharacter(rune.Value))
            {
                characters++;
                insideWord = false;
            }
            else if (Rune.IsLetterOrDigit(rune))
            {
                if (!insideWord) words++;
                insideWord = true;
            }
            else if (Rune.GetUnicodeCategory(rune) is not
                (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark))
            {
                insideWord = false;
            }
        }

        var minutes = (double)characters / CharactersPerMinute + (double)words / WordsPerMinute;
        return Math.Max(1, (int)Math.Ceiling(minutes));
    }

    private static bool IsEastAsianCharacter(int value) => value is
        >= 0x3400 and <= 0x4DBF or >= 0x4E00 and <= 0x9FFF or
        >= 0xF900 and <= 0xFAFF or >= 0x20000 and <= 0x323AF or
        >= 0x3041 and <= 0x3096 or >= 0x30A1 and <= 0x30FA or
        >= 0xAC00 and <= 0xD7A3;
}
