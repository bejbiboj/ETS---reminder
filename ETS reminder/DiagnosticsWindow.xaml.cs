using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace ETS_reminder;

public partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindow()
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        RefreshStatus();
    }

    internal static bool CanAccess()
    {
        var user = Environment.UserName;
        var p = UserProfile.Instance;
        var m = p?.Email?.ToLowerInvariant() ?? "";
        return user.Equals("v-negrokanic", StringComparison.OrdinalIgnoreCase)
            && m.Contains("nemanja.grokanic");
    }

    private void RefreshStatus()
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;

        StatusLabel.Text = $"{profile.DisplayName} | {profile.TotalCoins} ({profile.BonusCoins}) | L{profile.Level}";
        CoinAmountBox.Text = profile.BonusCoins.ToString();
        StreakBox.Text = profile.LongestStreak.ToString();
        AchievementStatusText.Text = $"{profile.UnlockedAchievements.Count}/{AchievementManager.AllAchievements.Length}";
        ThemeStatusText.Text = $"{profile.UnlockedThemes.Count}/{ShopCatalog.Themes.Length}";
    }

    private void SetCoins_Click(object sender, RoutedEventArgs e)
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;
        if (int.TryParse(CoinAmountBox.Text.Trim(), out var v) && v >= 0)
        { profile.BonusCoins = v; profile.Save(); StatsEngine.Calculate(); RefreshStatus(); }
    }

    private void AddCoins_Click(object sender, RoutedEventArgs e)
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string t && int.TryParse(t, out var v))
        { profile.BonusCoins += v; profile.Save(); StatsEngine.Calculate(); RefreshStatus(); }
    }

    private void UnlockAllAchievements_Click(object sender, RoutedEventArgs e)
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;
        foreach (var a in AchievementManager.AllAchievements)
            if (!profile.UnlockedAchievements.Contains(a.Id)) profile.UnlockedAchievements.Add(a.Id);
        profile.Save(); RefreshStatus();
    }

    private void ResetAchievements_Click(object sender, RoutedEventArgs e)
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;
        profile.UnlockedAchievements.Clear(); profile.Save(); RefreshStatus();
    }

    private void UnlockAllThemes_Click(object sender, RoutedEventArgs e)
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;
        foreach (var t in ShopCatalog.Themes)
            if (!profile.UnlockedThemes.Contains(t.Id)) profile.UnlockedThemes.Add(t.Id);
        profile.Save(); RefreshStatus();
    }

    private void ResetThemes_Click(object sender, RoutedEventArgs e)
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;
        profile.UnlockedThemes = ["default_dark", "default_light"];
        AppSettings.Instance.ActiveThemeId = "default_dark";
        AppSettings.Instance.Save();
        ThemeManager.ApplyTheme(AppSettings.Instance.DarkMode);
        profile.Save(); RefreshStatus();
    }

    private void SetStreak_Click(object sender, RoutedEventArgs e)
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;
        if (int.TryParse(StreakBox.Text.Trim(), out var v) && v >= 0)
        { profile.LongestStreak = v; profile.Save(); RefreshStatus(); }
    }
}
