using RazorFolio.Core.Assets;

namespace RazorFolio.Themes.Default;

public sealed class DefaultStylesheet : IThemeAsset
{
    public string RelativePath => "assets/razorfolio.css";

    public Stream OpenRead() => typeof(DefaultStylesheet).Assembly
        .GetManifestResourceStream("RazorFolio.Themes.Default.theme.css")
        ?? throw new InvalidOperationException("The embedded default stylesheet was not found.");
}
