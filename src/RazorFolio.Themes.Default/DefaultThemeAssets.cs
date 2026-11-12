using RazorFolio.Core.Assets;

namespace RazorFolio.Themes.Default;

public static class DefaultThemeAssets
{
    public static IReadOnlyList<IThemeAsset> All { get; } = Array.AsReadOnly<IThemeAsset>(
    [
        new EmbeddedAsset("assets/razorfolio.css", "theme.css", ThemeAssetKind.Stylesheet),
        new EmbeddedAsset("assets/code.css", "code.css", ThemeAssetKind.Stylesheet),
        new EmbeddedAsset("assets/theme.js", "theme.js", ThemeAssetKind.Script),
        new EmbeddedAsset("assets/highlight.min.js", "highlight.min.js", ThemeAssetKind.ContentScript),
        new EmbeddedAsset("assets/code-blocks.js", "code-blocks.js", ThemeAssetKind.ContentScript),
        new EmbeddedAsset("assets/highlight-LICENSE.txt", "highlight-LICENSE.txt", ThemeAssetKind.Other)
    ]);

    private sealed record EmbeddedAsset(string RelativePath, string ResourceName, ThemeAssetKind Kind) : IThemeAsset
    {
        public Stream OpenRead() => typeof(DefaultThemeAssets).Assembly
            .GetManifestResourceStream($"RazorFolio.Themes.Default.{ResourceName}")
            ?? throw new InvalidOperationException($"The embedded theme asset '{ResourceName}' was not found.");
    }
}
