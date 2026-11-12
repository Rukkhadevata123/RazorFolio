namespace RazorFolio.Core.Configuration;

internal static class ThemeConfiguration
{
    public static ThemeOptions Normalize(ThemeOptions? options, string configurationPath)
    {
        options ??= new ThemeOptions();
        // JSON may assign null even to a non-nullable property.
        var brand = (BrandOptions?)options.Brand ?? new BrandOptions();
        var hero = options.Hero;
        var profile = options.Profile;
        if (hero is not null)
        {
            var background = (BackgroundOptions?)hero.Background ?? new BackgroundOptions();
            if (background.Mode is not ("none" or "image" or "bing"))
            {
                throw new InvalidDataException($"theme.hero.background.mode in '{configurationPath}' must be none, image or bing.");
            }
            if (background.Mode == "image" && string.IsNullOrWhiteSpace(background.Image))
            {
                throw new InvalidDataException($"theme.hero.background.image in '{configurationPath}' is required in image mode.");
            }
            hero = hero with
            {
                Title = Text(hero.Title),
                Description = Text(hero.Description),
                Image = Image(hero.Image, "hero.image"),
                Background = background with { Image = Image(background.Image, "hero.background.image") }
            };
        }
        if (profile is not null)
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new InvalidDataException($"theme.profile.name in '{configurationPath}' must not be empty.");
            }
            profile = profile with
            {
                Name = profile.Name.Trim(),
                Avatar = Image(profile.Avatar, "profile.avatar"),
                Description = Text(profile.Description),
                Bio = Text(profile.Bio),
                Links = NavigationConfiguration.Normalize(profile.Links, configurationPath, "theme.profile.links")
            };
        }

        return options with
        {
            Brand = new BrandOptions { Text = Text(brand.Text), Image = Image(brand.Image, "brand.image") },
            Hero = hero,
            Profile = profile,
            Sidebar = NavigationConfiguration.Normalize(options.Sidebar, configurationPath, "theme.sidebar"),
            Footer = Text(options.Footer)
        };

        string? Image(string? value, string field) => Text(value) is { } url
            ? NavigationConfiguration.Normalize([new NavigationItem(field, url)], configurationPath, "theme." + field)[0].Link
            : null;
    }

    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
