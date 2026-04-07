namespace ETS_reminder;

public record ShopTheme(
    string Id,
    string Name,
    string Description,
    int Price,
    string AccentColor,
    string PreviewDark1,
    string PreviewDark2,
    string PreviewAccent,
    bool IsFree = false);

public static class ShopCatalog
{
    public static readonly ShopTheme[] Themes =
    [
        new("default_dark",   "Classic Dark",     "The original dark theme",         0,  "#E67E22", "#1E1E1E", "#2D2D30", "#E67E22", IsFree: true),
        new("default_light",  "Classic Light",    "The original warm light theme",   0,  "#E67E22", "#FFF8F0", "#FDEBD0", "#E67E22", IsFree: true),
        new("midnight_blue",  "Midnight Blue",    "Deep ocean blues",                25, "#3498DB", "#0D1B2A", "#1B2838", "#3498DB"),
        new("forest_green",   "Forest Green",     "Calm forest vibes",               25, "#2ECC71", "#1A2E1A", "#243524", "#2ECC71"),
        new("royal_purple",   "Royal Purple",     "Majestic and elegant",            30, "#9B59B6", "#1A1025", "#251835", "#9B59B6"),
        new("cherry_red",     "Cherry Red",       "Bold and energetic",              30, "#E74C3C", "#1E1012", "#2D1A1D", "#E74C3C"),
        new("sunset_orange",  "Sunset Orange",    "Warm sunset glow",                20, "#F39C12", "#1E1810", "#2D2418", "#F39C12"),
        new("cyberpunk",      "Cyberpunk",        "Neon pink futuristic",            40, "#E91E63", "#0A0A14", "#14142A", "#E91E63"),
        new("arctic",         "Arctic",           "Cool ice blue tones",             35, "#00BCD4", "#0A1A1E", "#142830", "#00BCD4"),
        new("golden",         "Golden",           "Premium gold luxury",             50, "#FFD700", "#1A1810", "#2A2618", "#FFD700"),
    ];
}
