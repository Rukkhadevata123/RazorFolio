namespace RazorFolio.Core;

public sealed class SiteBuildTarget
{
    public string SiteDirectory { get; }
    public string OutputDirectory { get; }
    public string ManifestPath { get; }
    public bool IncludeDrafts { get; }

    private SiteBuildTarget(string siteDirectory, bool development)
    {
        SiteDirectory = Path.GetFullPath(siteDirectory);
        var stateDirectory = Path.Combine(SiteDirectory, ".razorfolio");

        OutputDirectory = development
            ? Path.Combine(stateDirectory, "dev")
            : Path.Combine(SiteDirectory, "public");
        ManifestPath = Path.Combine(stateDirectory, development ? "dev-outputs.json" : "outputs.json");
        IncludeDrafts = development;
    }

    public static SiteBuildTarget Production(string siteDirectory) => new(siteDirectory, development: false);

    public static SiteBuildTarget Development(string siteDirectory) => new(siteDirectory, development: true);
}
