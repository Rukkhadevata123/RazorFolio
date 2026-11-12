using System.Globalization;

namespace RazorFolio.Core.Rendering;

public sealed record YearGroup(int? Year, IReadOnlyList<ArticleLink> Articles)
{
    public string Id => Year is { } year ? "year-" + year.ToString("D4", CultureInfo.InvariantCulture) : "undated";
}
