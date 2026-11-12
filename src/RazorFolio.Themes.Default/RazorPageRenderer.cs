using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using RazorFolio.Core.Rendering;

namespace RazorFolio.Themes.Default;

public sealed class RazorPageRenderer(
    IRazorViewEngine viewEngine,
    ITempDataProvider tempDataProvider,
    IServiceProvider services) : IPageRenderer
{
    public Task<string> RenderAsync(ArticlePage page, CancellationToken cancellationToken = default)
    {
        return RenderViewAsync("/Views/Article.cshtml", page, cancellationToken);
    }

    public Task<string> RenderAsync(HomePage page, CancellationToken cancellationToken = default)
    {
        return RenderViewAsync("/Views/Home.cshtml", page, cancellationToken);
    }

    public Task<string> RenderAsync(TagArchivePage page, CancellationToken cancellationToken = default)
    {
        return RenderViewAsync("/Views/Tags.cshtml", page, cancellationToken);
    }

    private async Task<string> RenderViewAsync<TModel>(
        string viewPath,
        TModel page,
        CancellationToken cancellationToken) where TModel : IPageModel
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = viewEngine.GetView(null, viewPath, isMainPage: true);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Precompiled theme view not found: {viewPath}");
        }

        // MVC views need a request context even when rendering without an HTTP server.
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            RequestAborted = cancellationToken
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = page
        };

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var viewContext = new ViewContext(
            actionContext,
            result.View,
            viewData,
            new TempDataDictionary(httpContext, tempDataProvider),
            writer,
            new HtmlHelperOptions());

        await result.View.RenderAsync(viewContext);
        cancellationToken.ThrowIfCancellationRequested();
        return writer.ToString();
    }
}
