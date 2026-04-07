using System.Windows.Media;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

namespace ETS_reminder;

public static class ThemeManager
{
    public static void ApplyTheme(bool isDarkMode)
    {
        var themeId = AppSettings.Instance.ActiveThemeId;

        // If a custom theme is active and it's dark mode, apply it
        if (isDarkMode && !string.IsNullOrEmpty(themeId) && themeId != "default_dark" && themeId != "default_light")
        {
            var shopTheme = ShopCatalog.Themes.FirstOrDefault(t => t.Id == themeId);
            if (shopTheme != null)
            {
                ApplyCustomDarkTheme(shopTheme);
                return;
            }
        }

        var resources = Application.Current.Resources;

        if (isDarkMode)
        {
            resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
            resources["SidePanelBackground"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x28));
            resources["MenuBackground"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
            resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x42));
            resources["TextColor"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            resources["SubTextColor"] = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
            resources["ContentText"] = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            resources["ListBackground"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x28));
            resources["ListItemBorder"] = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x42));
            resources["ListItemHover"] = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x42));
            resources["TextBoxBackground"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
            resources["TextBoxForeground"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            resources["TextBoxBorder"] = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x42));
            resources["DisabledButtonBackground"] = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x48));
        }
        else
        {
            resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xF0));
            resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(0xFD, 0xEB, 0xD0));
            resources["SidePanelBackground"] = new SolidColorBrush(Color.FromRgb(0xFD, 0xF2, 0xE9));
            resources["MenuBackground"] = new SolidColorBrush(Color.FromRgb(0xFD, 0xEB, 0xD0));
            resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xC5, 0xA0));
            resources["TextColor"] = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50));
            resources["SubTextColor"] = new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D));
            resources["ContentText"] = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50));
            resources["ListBackground"] = new SolidColorBrush(Colors.White);
            resources["ListItemBorder"] = new SolidColorBrush(Color.FromRgb(0xF0, 0xE0, 0xC8));
            resources["ListItemHover"] = new SolidColorBrush(Color.FromRgb(0xFD, 0xEB, 0xD0));
            resources["TextBoxBackground"] = new SolidColorBrush(Colors.White);
            resources["TextBoxForeground"] = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50));
            resources["TextBoxBorder"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xC5, 0xA0));
            resources["DisabledButtonBackground"] = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        }
    }

    private static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromRgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    private static Color Lighten(Color c, byte amount)
    {
        return Color.FromRgb(
            (byte)Math.Min(255, c.R + amount),
            (byte)Math.Min(255, c.G + amount),
            (byte)Math.Min(255, c.B + amount));
    }

    private static Color Darken(Color c, byte amount)
    {
        return Color.FromRgb(
            (byte)Math.Max(0, c.R - amount),
            (byte)Math.Max(0, c.G - amount),
            (byte)Math.Max(0, c.B - amount));
    }

    public static void ApplyCustomDarkTheme(ShopTheme theme)
    {
        var resources = Application.Current.Resources;
        var bg1 = ParseHex(theme.PreviewDark1);
        var bg2 = ParseHex(theme.PreviewDark2);
        var accent = ParseHex(theme.AccentColor);

        resources["WindowBackground"] = new SolidColorBrush(bg1);
        resources["PanelBackground"] = new SolidColorBrush(bg2);
        resources["SidePanelBackground"] = new SolidColorBrush(Darken(bg2, 8));
        resources["MenuBackground"] = new SolidColorBrush(bg2);
        resources["BorderColor"] = new SolidColorBrush(Lighten(bg2, 20));
        resources["TextColor"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        resources["SubTextColor"] = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
        resources["ContentText"] = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        resources["ListBackground"] = new SolidColorBrush(Darken(bg2, 8));
        resources["ListItemBorder"] = new SolidColorBrush(Lighten(bg2, 20));
        resources["ListItemHover"] = new SolidColorBrush(Lighten(bg2, 20));
        resources["TextBoxBackground"] = new SolidColorBrush(bg2);
        resources["TextBoxForeground"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        resources["TextBoxBorder"] = new SolidColorBrush(Lighten(bg2, 20));
        resources["DisabledButtonBackground"] = new SolidColorBrush(Lighten(bg2, 30));
    }
}
