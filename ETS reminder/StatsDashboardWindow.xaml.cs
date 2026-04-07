using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ETS_reminder;

public partial class StatsDashboardWindow : Window
{
    public StatsDashboardWindow()
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        LoadStats();
    }

    private void LoadStats()
    {
        var profile = UserProfile.Instance;
        var stats = StatsEngine.Calculate();

        // Profile header
        if (profile != null)
        {
            ProfileName.Text = profile.DisplayName;
            ProfileLevel.Text = $"Level {profile.Level}";
            ProfileTitle.Text = $" — {profile.LevelTitle}";
            ProfileRole.Text = profile.Role;
            AvatarInitials.Text = profile.Initials;
            AvatarBorder.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(profile.AvatarColor));

            // Level progress
            var prevLevel = profile.Level > 1 ? profile.Level switch
            {
                2 => 10, 3 => 30, 4 => 60, 5 => 100,
                6 => 150, 7 => 220, 8 => 300, 9 => 400, 10 => 500,
                _ => 0
            } : 0;
            var coinsInLevel = profile.TotalCoins - prevLevel;
            var coinsNeeded = profile.CoinsForNextLevel - prevLevel;
            var progress = coinsNeeded > 0 ? Math.Min((double)coinsInLevel / coinsNeeded, 1.0) : 1.0;

            LevelProgressBar.Width = progress * 450;
            LevelProgressLabel.Text = profile.Level >= 10
                ? "Max level reached!"
                : $"{profile.TotalCoins} / {profile.CoinsForNextLevel} coins to Level {profile.Level + 1}";
        }

        // Stats
        CurrentStreakText.Text = stats.CurrentStreak.ToString();
        LongestStreakText.Text = stats.LongestStreak.ToString();
        TotalReportsText.Text = stats.TotalReports.ToString();
        TotalCoinsText.Text = stats.TotalCoins.ToString();
        CompletionRateText.Text = $"{stats.CompletionRatePercent}%";

        // Today
        if (stats.CoinsEarnedToday > 0)
        {
            TodayCoinsText.Text = $"+{stats.CoinsEarnedToday}";
            TodayCoinsDetail.Text = $"1 report + {stats.CoinsEarnedToday - 1} streak bonus";
        }
        else
        {
            TodayCoinsText.Text = "+0";
            TodayCoinsText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#7F8C8D"));
            TodayCoinsDetail.Text = "Fill your report to earn coins!";
        }
    }
}
