using RazorFolio.Core.Configuration;

namespace RazorFolio.Core.Rendering;

public sealed record ThemeView(
    BrandOptions Brand,
    HeroOptions? Hero,
    ProfileOptions? Profile,
    IReadOnlyList<NavigationLink> Sidebar,
    string? Footer)
{
    public static ThemeView Create(SiteConfiguration site, Func<string, string> resolve)
    {
        var options = site.Theme;
        var brand = new BrandOptions
        {
            Text = options.Brand.Text ?? site.Title,
            Image = ResolveImage(options.Brand.Image)
        };
        var hero = options.Hero is { } sourceHero ? sourceHero with
        {
            Title = sourceHero.Title ?? site.Title,
            Description = sourceHero.Description ?? site.Description,
            Image = ResolveImage(sourceHero.Image),
            Background = sourceHero.Background with { Image = ResolveImage(sourceHero.Background.Image) }
        } : null;
        var profile = options.Profile is { } sourceProfile ? sourceProfile with
        {
            Avatar = ResolveImage(sourceProfile.Avatar),
            Links = [.. sourceProfile.Links.Select(link => link with { Link = resolve(link.Link) })]
        } : null;
        var sidebar = options.Sidebar.Select(link => new NavigationLink(link.Text, resolve(link.Link))).ToArray();
        return new ThemeView(brand, hero, profile, sidebar, options.Footer);

        string? ResolveImage(string? url) => url is null ? null : resolve(url);
    }
}
