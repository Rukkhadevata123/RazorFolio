namespace RazorFolio.Core.Assets;

public interface IThemeAsset
{
    string RelativePath { get; }
    ThemeAssetKind Kind { get; }

    // Each call returns a new stream owned by the caller.
    Stream OpenRead();
}
