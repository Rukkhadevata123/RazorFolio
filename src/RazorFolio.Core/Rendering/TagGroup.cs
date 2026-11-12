using System.Text;

namespace RazorFolio.Core.Rendering;

public sealed record TagGroup(string Name, IReadOnlyList<ArticleLink> Articles)
{
    public string Id => GetId(Name);

    // Encode names to keep anchors stable and free of whitespace or URL delimiters.
    public static string GetId(string name) => "tag-" + Convert.ToHexStringLower(Encoding.UTF8.GetBytes(name));
}
