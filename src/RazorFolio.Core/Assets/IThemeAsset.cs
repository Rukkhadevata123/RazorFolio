namespace RazorFolio.Core.Assets;

public interface IThemeAsset
{
    string RelativePath { get; }

    // Each call returns a new stream owned by the caller.
    Stream OpenRead();
}
