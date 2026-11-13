using RazorFolio.Core.Assets;

namespace RazorFolio.Themes.Default;

public static class DefaultThemeAssets
{
    public static IReadOnlyList<IThemeAsset> All { get; } = Array.AsReadOnly<IThemeAsset>(
    [
        new EmbeddedAsset("assets/razorfolio.css", "theme.css", ThemeAssetKind.Stylesheet),
        new EmbeddedAsset("assets/code.css", "code.css", ThemeAssetKind.Stylesheet),
        new EmbeddedAsset("assets/search.css", "search.css", ThemeAssetKind.Stylesheet),
        new EmbeddedAsset("assets/mermaid.css", "mermaid.css", ThemeAssetKind.Stylesheet),
        new EmbeddedAsset("assets/math.css", "math.css", ThemeAssetKind.Stylesheet),
        new EmbeddedAsset("assets/theme.js", "theme.js", ThemeAssetKind.Script),
        new EmbeddedAsset("assets/search.js", "search.js", ThemeAssetKind.Script),
        new EmbeddedAsset("assets/highlight.min.js", "highlight.min.js", ThemeAssetKind.ContentScript),
        new EmbeddedAsset("assets/code-blocks.js", "code-blocks.js", ThemeAssetKind.ContentScript),
        new EmbeddedAsset("assets/mermaid.js", "mermaid.js", ThemeAssetKind.ContentScript),
        new EmbeddedAsset("assets/math.js", "math.js", ThemeAssetKind.ContentScript),
        new EmbeddedAsset("assets/katex/katex.min.js", "katex.min.js", ThemeAssetKind.Other),
        new EmbeddedAsset("assets/katex/katex.min.css", "katex.min.css", ThemeAssetKind.Other),
        new EmbeddedAsset("assets/katex/LICENSE.txt", "katex-LICENSE.txt", ThemeAssetKind.Other),
        .. GetMathFontAssets(),
        new EmbeddedAsset("assets/mermaid.tiny.js", "mermaid.tiny.js", ThemeAssetKind.Other),
        new EmbeddedAsset("assets/mermaid-LICENSE.txt", "mermaid-LICENSE.txt", ThemeAssetKind.Other),
        new EmbeddedAsset("assets/highlight-LICENSE.txt", "highlight-LICENSE.txt", ThemeAssetKind.Other)
    ]);

    private static IEnumerable<IThemeAsset> GetMathFontAssets()
    {
        const string resourcePrefix = "RazorFolio.Themes.Default.katex.fonts.";
        return typeof(DefaultThemeAssets).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(name => new EmbeddedAsset(
                "assets/katex/fonts/" + name[resourcePrefix.Length..],
                "katex.fonts." + name[resourcePrefix.Length..], ThemeAssetKind.Other));
    }

    private sealed record EmbeddedAsset(string RelativePath, string ResourceName, ThemeAssetKind Kind) : IThemeAsset
    {
        public Stream OpenRead() => typeof(DefaultThemeAssets).Assembly
            .GetManifestResourceStream($"RazorFolio.Themes.Default.{ResourceName}")
            ?? throw new InvalidOperationException($"The embedded theme asset '{ResourceName}' was not found.");
    }
}
