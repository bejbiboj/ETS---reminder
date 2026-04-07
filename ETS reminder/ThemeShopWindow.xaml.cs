using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace ETS_reminder;

public partial class ThemeShopWindow : Window
{
    public ThemeShopWindow()
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        LoadShop();
    }

    private void LoadShop()
    {
        var profile = UserProfile.Instance;
        CoinBalanceText.Text = profile?.TotalCoins.ToString() ?? "0";

        var activeThemeId = AppSettings.Instance.ActiveThemeId;
        var unlocked = profile?.UnlockedThemes ?? ["default_dark", "default_light"];

        var items = ShopCatalog.Themes.Select(t =>
        {
            var isOwned = unlocked.Contains(t.Id);
            var isActive = t.Id == activeThemeId;
            var canAfford = (profile?.TotalCoins ?? 0) >= t.Price;

            string buttonText;
            SolidColorBrush buttonColor;
            bool buttonEnabled;

            if (isActive)
            {
                buttonText = "Equipped";
                buttonColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D"));
                buttonEnabled = false;
            }
            else if (isOwned)
            {
                buttonText = "Equip";
                buttonColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
                buttonEnabled = true;
            }
            else if (canAfford)
            {
                buttonText = $"Buy ({t.Price})";
                buttonColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
                buttonEnabled = true;
            }
            else
            {
                buttonText = $"Buy ({t.Price})";
                buttonColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D"));
                buttonEnabled = false;
            }

            return new ThemeShopDisplayItem
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                PriceText = t.IsFree ? "Free" : $"{t.Price} coins",
                PriceColor = t.IsFree || isOwned
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"))
                    : canAfford
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"))
                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")),
                NameColor = isActive
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(t.AccentColor))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0")),
                PreviewColor1 = (Color)ColorConverter.ConvertFromString(t.PreviewDark1),
                PreviewColor2 = (Color)ColorConverter.ConvertFromString(t.PreviewDark2),
                AccentColorValue = (Color)ColorConverter.ConvertFromString(t.AccentColor),
                ButtonText = buttonText,
                ButtonColor = buttonColor,
                ButtonEnabled = buttonEnabled,
                ActiveLabel = isActive ? "ACTIVE" : "",
                BorderColor = isActive
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(t.AccentColor))
                    : new SolidColorBrush(Colors.Transparent),
                BorderWidth = isActive ? new System.Windows.Thickness(2) : new System.Windows.Thickness(0)
            };
        }).ToList();

        ThemeList.ItemsSource = items;
    }

    private void ThemeAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string themeId)
            return;

        var profile = UserProfile.Instance;
        if (profile == null) return;

        var theme = ShopCatalog.Themes.FirstOrDefault(t => t.Id == themeId);
        if (theme == null) return;

        var isOwned = profile.UnlockedThemes.Contains(themeId);

        if (!isOwned)
        {
            // Buy
            var result = MessageBox.Show(
                $"Buy \"{theme.Name}\" for {theme.Price} coins?",
                "Confirm Purchase", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            if (!profile.SpendCoins(theme.Price))
            {
                MessageBox.Show("Not enough coins!", "Insufficient Coins",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            profile.UnlockedThemes.Add(themeId);
            profile.Save();
        }

        // Equip
        AppSettings.Instance.ActiveThemeId = themeId;
        var isDark = themeId != "default_light";
        AppSettings.Instance.DarkMode = isDark;
        AppSettings.Instance.Save();
        ThemeManager.ApplyTheme(isDark);

        LoadShop(); // Refresh UI
    }
}

public class ThemeShopDisplayItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PriceText { get; set; } = "";
    public SolidColorBrush PriceColor { get; set; } = new(Colors.Gray);
    public SolidColorBrush NameColor { get; set; } = new(Colors.White);
    public Color PreviewColor1 { get; set; }
    public Color PreviewColor2 { get; set; }
    public Color AccentColorValue { get; set; }
    public string ButtonText { get; set; } = "";
    public SolidColorBrush ButtonColor { get; set; } = new(Colors.Gray);
    public bool ButtonEnabled { get; set; }
    public string ActiveLabel { get; set; } = "";
    public SolidColorBrush BorderColor { get; set; } = new(Colors.Transparent);
    public System.Windows.Thickness BorderWidth { get; set; }
}
