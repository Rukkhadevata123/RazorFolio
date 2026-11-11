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
    public async Task<string> RenderAsync(ArticlePage page, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = viewEngine.GetView(null, "/Views/Article.cshtml", isMainPage: true);

        if (!result.Success)
        {
            throw new InvalidOperationException("The precompiled default article view could not be found.");
        }

        // MVC views need a request context even when rendering without an HTTP server.
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            RequestAborted = cancellationToken
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary<ArticlePage>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
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
