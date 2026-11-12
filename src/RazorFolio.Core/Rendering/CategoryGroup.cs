using System.Text;

namespace RazorFolio.Core.Rendering;

public sealed record CategoryGroup(string Name, IReadOnlyList<ArticleLink> Articles)
{
    public string Id => GetId(Name);

    public static string GetId(string name) => "category-" + Convert.ToHexStringLower(Encoding.UTF8.GetBytes(name));
}
