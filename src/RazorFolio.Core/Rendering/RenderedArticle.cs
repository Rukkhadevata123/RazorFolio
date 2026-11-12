namespace RazorFolio.Core.Rendering;

public sealed record RenderedArticle(string BodyHtml, string? TitleHtml, IReadOnlyList<HeadingLink> Headings);
