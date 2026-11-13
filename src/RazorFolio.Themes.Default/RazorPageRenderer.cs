using RazorFolio.Core.Rendering;
using RazorFolio.Themes.Default.Generated;
using RazorFolio.Themes.Default.Rendering;

namespace RazorFolio.Themes.Default;

public sealed class RazorPageRenderer : IPageRenderer
{
    public Task<string> RenderAsync(ArticlePage page, CancellationToken cancellationToken = default) =>
        RenderPageAsync(new Article { Model = page }, cancellationToken);

    public Task<string> RenderAsync(HomePage page, CancellationToken cancellationToken = default) =>
        RenderPageAsync(new Home { Model = page }, cancellationToken);

    public Task<string> RenderAsync(TagArchivePage page, CancellationToken cancellationToken = default) =>
        RenderPageAsync(new Tags { Model = page }, cancellationToken);

    public Task<string> RenderAsync(TagPage page, CancellationToken cancellationToken = default) =>
        RenderPageAsync(new Tag { Model = page }, cancellationToken);

    public Task<string> RenderAsync(CategoryArchivePage page, CancellationToken cancellationToken = default) =>
        RenderPageAsync(new Categories { Model = page }, cancellationToken);

    public Task<string> RenderAsync(YearArchivePage page, CancellationToken cancellationToken = default) =>
        RenderPageAsync(new Archives { Model = page }, cancellationToken);

    private static async Task<string> RenderPageAsync<TModel>(
        PageTemplate<TModel> template, CancellationToken cancellationToken) where TModel : IPageModel
    {
        var body = await template.RenderAsync(cancellationToken);
        var layout = new Shared__Layout
        {
            Model = new LayoutModel(template.Model.Context, template.Metadata, body)
        };
        return (await layout.RenderAsync(cancellationToken)).Value;
    }
}
