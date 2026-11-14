using System.Net;
using System.Text.RegularExpressions;

namespace RazorFolio.Core.Diagnostics;

internal static partial class HtmlAnchorReader
{
    public static HashSet<string> Read(string html)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var templateDepth = 0;
        foreach (Match token in HtmlToken().Matches(html))
        {
            var tag = token.Groups["tag"].Value;
            if (tag.Length == 0)
            {
                continue;
            }

            var closing = token.Groups["closing"].Success;
            if (tag.Equals("template", StringComparison.OrdinalIgnoreCase))
            {
                if (closing)
                {
                    templateDepth = Math.Max(0, templateDepth - 1);
                    continue;
                }

                templateDepth++;
                if (templateDepth > 1)
                {
                    continue;
                }
            }
            else if (closing || templateDepth > 0)
            {
                continue;
            }

            // Read complete attributes so text inside a quoted value cannot become a false ID.
            var hasId = false;
            var hasName = false;
            foreach (Match attribute in HtmlAttribute().Matches(token.Groups["attributes"].Value))
            {
                var name = attribute.Groups["name"].Value;
                var isId = name.Equals("id", StringComparison.OrdinalIgnoreCase);
                var isAnchorName = tag.Equals("a", StringComparison.OrdinalIgnoreCase)
                    && name.Equals("name", StringComparison.OrdinalIgnoreCase);
                if ((!isId && !isAnchorName) || (isId && hasId) || (isAnchorName && hasName))
                {
                    continue;
                }

                hasId |= isId;
                hasName |= isAnchorName;
                var value = WebUtility.HtmlDecode(attribute.Groups["value"].Value);
                if (value.Length > 0)
                {
                    anchors.Add(value);
                }
            }
        }

        return anchors;
    }

    private const string Attributes = """(?<attributes>(?:\s+[^\s/>=]+(?:\s*=\s*(?:"[^"]*"|'[^']*'|[^\s>]+))?)*\s*)""";

    // Consume comments and raw-text contents as single tokens. Only opening tags expose attributes.
    [GeneratedRegex(@"<!--[\s\S]*?(?:-->|$)|<![^>]*>|<\?[^>]*>|"
        + @"<(?<tag>script|style|textarea|title|xmp|iframe|noembed|noframes)" + Attributes
        + @">[\s\S]*?(?:</\k<tag>\s*>|$)|"
        + @"<(?<closing>/)?(?<tag>[a-zA-Z][a-zA-Z0-9:-]*)" + Attributes + @"/?>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlToken();

    [GeneratedRegex("""(?<name>[^\s/=]+)(?:\s*=\s*(?:"(?<value>[^"]*)"|'(?<value>[^']*)'|(?<value>[^\s>]+)))?""", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlAttribute();
}
