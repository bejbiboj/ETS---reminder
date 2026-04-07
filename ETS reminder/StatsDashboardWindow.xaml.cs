using System.Windows;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            ProfileTitle.Text = $" \u2014 {profile.LevelTitle}";
            ProfileRole.Text = profile.Role;
            AvatarRenderer.Render(AvatarBorder, AvatarInitials, profile, 24);

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

        // Animate streak if at a milestone
        AnimateStreak(stats.CurrentStreak);

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

        // This Week
        LoadWeekly();

        // Achievements
        LoadAchievements(profile);
    }

    private void AnimateStreak(int streak)
    {
        if (streak < 3)
        {
            MilestoneBanner.Opacity = 0;
            return;
        }

        // Determine milestone text
        string? milestoneLabel = streak switch
        {
            >= 100 => "UNBREAKABLE!",
            >= 50 => "IRON WILL!",
            >= 20 => "LEGENDARY!",
            >= 10 => "UNSTOPPABLE!",
            >= 5 => "ON FIRE!",
            >= 3 => "WARMING UP!",
            _ => null
        };

        // Bounce animation on the streak number
        var numberBounce = new DoubleAnimation
        {
            From = 0.5, To = 1.0,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 4 }
        };
        StreakNumberScale.BeginAnimation(ScaleTransform.ScaleXProperty, numberBounce);
        StreakNumberScale.BeginAnimation(ScaleTransform.ScaleYProperty, numberBounce);

        // Fire emoji pulse animation
        var firePulse = new DoubleAnimation
        {
            From = 1.0, To = 1.4,
            Duration = TimeSpan.FromMilliseconds(300),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(streak >= 10 ? 3 : 2),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        FireScale.BeginAnimation(ScaleTransform.ScaleXProperty, firePulse);
        FireScale.BeginAnimation(ScaleTransform.ScaleYProperty, firePulse);

        // Glow: color animation on the streak number (orange -> gold -> orange)
        if (streak >= 5)
        {
            var glow = new ColorAnimation
            {
                From = (Color)ColorConverter.ConvertFromString("#E67E22"),
                To = (Color)ColorConverter.ConvertFromString("#FFD700"),
                Duration = TimeSpan.FromMilliseconds(500),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)
            };
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
            CurrentStreakText.Foreground = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, glow);
        }

        // Milestone banner slide-in
        if (milestoneLabel != null)
        {
            MilestoneText.Text = milestoneLabel;

            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(300)
            };
            MilestoneBanner.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            // Change banner color based on streak tier
            var bannerColor = streak switch
            {
                >= 50 => "#FFD700",
                >= 20 => "#9B59B6",
                >= 10 => "#E74C3C",
                >= 5 => "#E67E22",
                _ => "#3498DB"
            };
            MilestoneBanner.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(bannerColor));
        }
        else
        {
            MilestoneBanner.Opacity = 0;
        }
    }

    private void LoadWeekly()
    {
        var weekly = StatsEngine.CalculateWeekly();

        WeekDateRange.Text = $"({weekly.WeekStart:MMM dd} \u2013 {weekly.WeekEnd:MMM dd})";

        // Day indicators
        var dayBorders = new[] { DayMon, DayTue, DayWed, DayThu, DayFri };
        var dayChecks = new[] { DayMonCheck, DayTueCheck, DayWedCheck, DayThuCheck, DayFriCheck };
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone));

        for (int i = 0; i < 5; i++)
        {
            var day = weekly.WeekStart.AddDays(i);
            if (weekly.FiledDates.Contains(day))
            {
                dayBorders[i].Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#2ECC71"));
                dayChecks[i].Visibility = Visibility.Visible;
            }
            else if (day < today && weekly.MissedDates.Contains(day))
            {
                dayBorders[i].Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
            else if (day == today)
            {
                dayBorders[i].Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E67E22"));
            }
            // Future days stay as default BorderColor
        }

        // Summary text
        var parts = new List<string>
        {
            $"{weekly.ReportsFiled}/{weekly.WeekdaysSoFar} reports filed"
        };
        if (weekly.CoinsEarned > 0)
            parts.Add($"+{weekly.CoinsEarned} coins earned");
        if (weekly.IsPerfectWeek)
            parts.Add("Perfect Week!");

        WeekSummaryText.Text = string.Join("  |  ", parts);
    }

    private void LoadAchievements(UserProfile? profile)
    {
        var unlocked = profile?.UnlockedAchievements ?? [];
        var unlockedCount = unlocked.Count;
        var totalCount = AchievementManager.AllAchievements.Length;

        AchievementCountText.Text = $" ({unlockedCount}/{totalCount})";

        var items = AchievementManager.AllAchievements.Select(a =>
        {
            var isUnlocked = unlocked.Contains(a.Id);
            return new AchievementDisplayItem
            {
                Icon = System.Net.WebUtility.HtmlDecode(a.Icon),
                Name = a.Name,
                Description = isUnlocked ? a.Description : "???",
                Opacity = isUnlocked ? 1.0 : 0.35,
                NameColor = isUnlocked
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D"))
            };
        }).ToList();

        AchievementsList.ItemsSource = items;
    }
}

public class AchievementDisplayItem
{
    public string Icon { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double Opacity { get; set; } = 1.0;
    public SolidColorBrush NameColor { get; set; } = new(Colors.Gray);
}
