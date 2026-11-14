namespace RazorFolio.Core.Assets;

public interface IThemeAsset
{
    string RelativePath { get; }
    ThemeAssetKind Kind { get; }

    // Literal relative URLs in this asset that refer to other theme assets.
    IReadOnlyList<string> References => [];

    // Each call returns a new stream owned by the caller.
    Stream OpenRead();
}
