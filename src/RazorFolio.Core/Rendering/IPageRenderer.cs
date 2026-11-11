namespace RazorFolio.Core.Rendering;

public interface IPageRenderer
{
    Task<string> RenderAsync(ArticlePage page, CancellationToken cancellationToken = default);
}
