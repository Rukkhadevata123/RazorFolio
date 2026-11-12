namespace RazorFolio.Core.Configuration;

public sealed record ThemeOptions
{
    public BrandOptions Brand { get; init; } = new();
    public HeroOptions? Hero { get; init; }
    public ProfileOptions? Profile { get; init; }
    public IReadOnlyList<NavigationItem> Sidebar { get; init; } = [];
    public string? Footer { get; init; }
}

public sealed record BrandOptions
{
    public string? Text { get; init; }
    public string? Image { get; init; }
}

public sealed record HeroOptions
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Image { get; init; }
    public bool FullScreen { get; init; }
    public BackgroundOptions Background { get; init; } = new();
}

public sealed record BackgroundOptions
{
    public string Mode { get; init; } = "none";
    public string? Image { get; init; }
}

public sealed record ProfileOptions
{
    public string Name { get; init; } = "";
    public string? Avatar { get; init; }
    public string? Description { get; init; }
    public string? Bio { get; init; }
    public IReadOnlyList<NavigationItem> Links { get; init; } = [];
}
