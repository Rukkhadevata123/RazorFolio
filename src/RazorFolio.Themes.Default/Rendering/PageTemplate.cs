using RazorFolio.Core.Rendering;

namespace RazorFolio.Themes.Default.Rendering;

internal abstract class PageTemplate<TModel> : RazorTemplate<TModel> where TModel : IPageModel
{
    public PageMetadata Metadata { get; } = new();
}

internal sealed class PageMetadata
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public bool NoIndex { get; set; }
    public string CssClass { get; set; } = "archive-shell";
}

internal sealed record LayoutModel(PageContext Context, PageMetadata Metadata, HtmlContent Body);
