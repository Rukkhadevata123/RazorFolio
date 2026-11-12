using RazorFolio.Core.Assets;

namespace RazorFolio.Themes.Default;

public static class DefaultThemeAssets
{
    public static IReadOnlyList<IThemeAsset> All { get; } = Array.AsReadOnly<IThemeAsset>(
    [
        new EmbeddedAsset("assets/razorfolio.css", "theme.css"),
        new EmbeddedAsset("assets/code.css", "code.css"),
        new EmbeddedAsset("assets/highlight.min.js", "highlight.min.js"),
        new EmbeddedAsset("assets/code-blocks.js", "code-blocks.js"),
        new EmbeddedAsset("assets/highlight-LICENSE.txt", "highlight-LICENSE.txt")
    ]);

    private sealed record EmbeddedAsset(string RelativePath, string ResourceName) : IThemeAsset
    {
        public Stream OpenRead() => typeof(DefaultThemeAssets).Assembly
            .GetManifestResourceStream($"RazorFolio.Themes.Default.{ResourceName}")
            ?? throw new InvalidOperationException($"The embedded theme asset '{ResourceName}' was not found.");
    }
}
