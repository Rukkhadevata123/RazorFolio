namespace RazorFolio.Themes.Default.Rendering;

// Only renderer-produced markup or explicitly trusted author HTML may bypass encoding.
internal sealed record HtmlContent(string Value);
