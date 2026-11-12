namespace RazorFolio.Core.Rendering;

public interface IPageRenderer
{
    Task<string> RenderAsync(ArticlePage page, CancellationToken cancellationToken = default);

    Task<string> RenderAsync(HomePage page, CancellationToken cancellationToken = default);

    Task<string> RenderAsync(TagArchivePage page, CancellationToken cancellationToken = default);

    Task<string> RenderAsync(TagPage page, CancellationToken cancellationToken = default);

    Task<string> RenderAsync(CategoryArchivePage page, CancellationToken cancellationToken = default);

    Task<string> RenderAsync(YearArchivePage page, CancellationToken cancellationToken = default);
}
