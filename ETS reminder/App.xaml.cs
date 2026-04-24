using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;
using Application = System.Windows.Application;

namespace ETS_reminder;

public partial class App : Application
{
    private System.Windows.Forms.NotifyIcon _trayIcon = null!;
    private System.Windows.Threading.DispatcherTimer _timer = null!;
    public static TimeZoneInfo AppTimeZone => AppSettings.Instance.GetTimeZone();
    private bool _reportFilledToday;
    private DateOnly _lastReportDate;
    private static Mutex? _singleInstanceMutex;
    private bool _weeklySummaryShownThisWeek;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, "ETS_Reminder_SingleInstance", out bool isNewInstance);
        if (!isNewInstance)
        {
            System.Windows.MessageBox.Show("ETS Reminder is already running.",
                "Already Running", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Current.Shutdown();
            return;
        }

        ThemeManager.ApplyTheme(AppSettings.Instance.DarkMode);

        // Show profile setup on first launch
        if (!UserProfile.Exists)
        {
            var setup = new ProfileSetupWindow();
            if (setup.ShowDialog() != true)
            {
                Current.Shutdown();
                return;
            }
        }

        SetupTrayIcon();
        SetupTimer();

        ToastNotificationManagerCompat.OnActivated += OnToastActivated;

        // Show Log Viewer on startup if enabled in settings
        if (AppSettings.Instance.ShowLogViewerOnStartup)
        {
            ShowLogViewerWindow();
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "ETS Reminder",
            Visible = true
        };

        // Create tray icon
        _trayIcon.Icon = CreateAppIcon(32);

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Fill ETS Report", null, (_, _) => ShowReportWindow());
        contextMenu.Items.Add("Bulk Entry", null, (_, _) => ShowBulkEntryWindow());
        contextMenu.Items.Add("View Report Logs", null, (_, _) => ShowLogViewerWindow());
        contextMenu.Items.Add("Open Reports Folder", null, (_, _) => OpenReportsFolder());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Stats Dashboard", null, (_, _) => ShowStatsDashboard());
        contextMenu.Items.Add("Theme Shop", null, (_, _) => ShowThemeShop());
        contextMenu.Items.Add("Edit Profile", null, (_, _) => ShowEditProfile());
        contextMenu.Items.Add("Settings", null, (_, _) => ShowSettingsWindow());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApp());
        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, _) => ShowReportWindow();
    }

    private static System.Drawing.Icon CreateAppIcon(int size)
    {
        // Render at a larger size for quality, then scale down
        var renderSize = Math.Max(size, 64);
        using var bitmap = new System.Drawing.Bitmap(renderSize, renderSize);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // Rounded orange background
            var radius = renderSize * 0.15f;
            using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 245, 124, 0));
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            var rect = new System.Drawing.RectangleF(0, 0, renderSize, renderSize);
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            g.FillPath(bgBrush, path);

            // Draw "E" centered — cleaner than "ETS" at small sizes
            var fontSize = renderSize * 0.55f;
            using var font = new System.Drawing.Font("Segoe UI", fontSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
            using var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            using var format = new System.Drawing.StringFormat
            {
                Alignment = System.Drawing.StringAlignment.Center,
                LineAlignment = System.Drawing.StringAlignment.Center
            };
            g.DrawString("E", font, textBrush, new System.Drawing.RectangleF(0, 0, renderSize, renderSize), format);
        }

        // Scale down to requested size if needed
        System.Drawing.Bitmap finalBitmap;
        if (renderSize != size)
        {
            finalBitmap = new System.Drawing.Bitmap(size, size);
            using var g2 = System.Drawing.Graphics.FromImage(finalBitmap);
            g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g2.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g2.DrawImage(bitmap, 0, 0, size, size);
        }
        else
        {
            finalBitmap = (System.Drawing.Bitmap)bitmap.Clone();
        }

        using (finalBitmap)
        {
            var hIcon = finalBitmap.GetHicon();
            try
            {
                return (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
    }

    public static System.Windows.Media.ImageSource GetWindowIcon()
    {
        using var icon = CreateAppIcon(64);
        using var bitmap = icon.ToBitmap();
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private void SetupTimer()
    {
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone);
        var today = DateOnly.FromDateTime(now);

        // Reset the flag for a new day
        if (today != _lastReportDate)
        {
            _reportFilledToday = false;
            _lastReportDate = today;

            // Reset weekly summary flag on Monday
            if (now.DayOfWeek == DayOfWeek.Monday)
                _weeklySummaryShownThisWeek = false;
        }

        // Only remind on weekdays (Mon-Fri)
        if (now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return;

        // Friday weekly summary — show once at reminder time
        if (now.DayOfWeek == DayOfWeek.Friday && !_weeklySummaryShownThisWeek)
        {
            var settings2 = AppSettings.Instance;
            var summaryMinutes = settings2.ReminderHour * 60 + settings2.ReminderMinute;
            var nowMinutes = now.Hour * 60 + now.Minute;
            if (nowMinutes >= summaryMinutes && nowMinutes < summaryMinutes + 5)
            {
                _weeklySummaryShownThisWeek = true;
                ShowWeeklySummaryToast();
            }
        }

        // Skip if already filled today (from memory)
        if (_reportFilledToday)
            return;

        // Check if report already exists on disk for today
        var existingReport = ReportStorage.LoadReport(now);
        if (!string.IsNullOrEmpty(existingReport))
        {
            _reportFilledToday = true;
            return;
        }

        // Start reminding at configured time, and re-remind every 5 minutes
        var settings = AppSettings.Instance;
        var reminderStartMinutes = settings.ReminderHour * 60 + settings.ReminderMinute;
        var currentMinutes = now.Hour * 60 + now.Minute;

        if (currentMinutes >= reminderStartMinutes)
        {
            var minutesSinceStart = currentMinutes - reminderStartMinutes;
            if (minutesSinceStart % 5 == 0)
            {
                ShowToastNotification();
            }
        }
    }

    private void ShowToastNotification()
    {
        new ToastContentBuilder()
            .AddArgument("action", "openReport")
            .AddText("\u23f0 ETS Reminder")
            .AddText("Don't forget to fill in your ETS daily report!")
            .AddText("This will keep reminding you every 5 minutes until you do it.")
            .AddButton(new ToastButton()
                .SetContent("Fill Report Now")
                .AddArgument("action", "openReport"))
            .AddButton(new ToastButton()
                .SetContent("Dismiss")
                .AddArgument("action", "dismiss"))
            .Show();
    }

    private void ShowWeeklySummaryToast()
    {
        var weekly = StatsEngine.CalculateWeekly();
        var stats = StatsEngine.Calculate();

        var streakText = stats.CurrentStreak > 0 ? $"Streak: {stats.CurrentStreak} days" : "No active streak";
        var perfText = weekly.IsPerfectWeek ? "\nPerfect Week! \u2B50" : "";

        new ToastContentBuilder()
            .AddArgument("action", "openStats")
            .AddText("\U0001F4CA Weekly Recap — Friday")
            .AddText($"Reports: {weekly.ReportsFiled}/{weekly.WeekdaysSoFar} | Coins: +{weekly.CoinsEarned} | {streakText}{perfText}")
            .AddButton(new ToastButton()
                .SetContent("View Stats")
                .AddArgument("action", "openStats"))
            .AddButton(new ToastButton()
                .SetContent("Dismiss")
                .AddArgument("action", "dismiss"))
            .Show();
    }

    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        var args = ToastArguments.Parse(e.Argument);
        if (args.TryGetValue("action", out var action))
        {
            if (action == "openReport")
                Current.Dispatcher.Invoke(ShowReportWindow);
            else if (action == "openStats")
                Current.Dispatcher.Invoke(ShowStatsDashboard);
        }
    }

    public void ShowReportWindow()
    {
        var existingWindow = Current.Windows.OfType<ReportWindow>().FirstOrDefault();
        if (existingWindow != null)
        {
            existingWindow.Activate();
            return;
        }

        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone);
        var window = new ReportWindow(now);
        window.ReportSaved += OnReportSaved;
        window.Show();
        window.Activate();
    }

    private void OnReportSaved(object? sender, EventArgs e)
    {
        _reportFilledToday = true;
        _lastReportDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone));
        CheckAchievements();
    }

    public static void CheckAchievements()
    {
        var stats = StatsEngine.Calculate();
        var newAchievements = AchievementManager.CheckAndUnlock(stats);
        foreach (var achievement in newAchievements)
        {
            AchievementManager.ShowAchievementToast(achievement);
        }
    }

    private void ShowLogViewerWindow()
    {
        var existingWindow = Current.Windows.OfType<LogViewerWindow>().FirstOrDefault();
        if (existingWindow != null)
        {
            existingWindow.Activate();
            return;
        }

        var window = new LogViewerWindow();
        window.Show();
        window.Activate();
    }

    public void ShowBulkEntryWindow()
    {
        var existingWindow = Current.Windows.OfType<BulkEntryWindow>().FirstOrDefault();
        if (existingWindow != null)
        {
            existingWindow.Activate();
            return;
        }

        var window = new BulkEntryWindow();
        window.EntriesSaved += (s, e) =>
        {
            _reportFilledToday = true;
            _lastReportDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone));
            CheckAchievements();
        };
        window.Show();
        window.Activate();
    }

    public void ShowSettingsWindow()
    {
        var existingWindow = Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
        if (existingWindow != null)
        {
            existingWindow.Activate();
            return;
        }

        var window = new SettingsWindow();
        window.ShowDialog();
    }

    public void ShowStatsDashboard()
    {
        var existing = Current.Windows.OfType<StatsDashboardWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = new StatsDashboardWindow();
        // Find an active owner window so the dashboard appears on top
        var owner = Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive && w is not StatsDashboardWindow);
        if (owner != null)
            window.Owner = owner;
        window.Show();
        window.Activate();
        window.Focus();
    }

    public void ShowThemeShop()
    {
        var existing = Current.Windows.OfType<ThemeShopWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = new ThemeShopWindow();
        var owner = Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive && w is not ThemeShopWindow);
        if (owner != null)
            window.Owner = owner;
        window.Show();
        window.Activate();
    }

    public void ShowEditProfile()
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;

        var setup = new ProfileSetupWindow(profile);
        setup.ShowDialog();
    }

    public static void OpenReportsFolder()
    {
        var folder = ReportStorage.GetReportsFolder();
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
        System.Diagnostics.Process.Start("explorer.exe", folder);
    }

    public void ExitApp()
    {
        ToastNotificationManagerCompat.Uninstall();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _timer.Stop();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        Current.Shutdown();
    }
}
