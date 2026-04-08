using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using ListBoxItem = System.Windows.Controls.ListBoxItem;

namespace ETS_reminder;

public partial class LogViewerWindow : Window
{
    private System.Windows.Threading.DispatcherTimer? _searchTimer;

    public LogViewerWindow()
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        AddEntryDatePicker.SelectedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone);
        LoadMonths();
        SetupSearchTimer();
        LoadProfileIndicator();
    }

    private void LoadProfileIndicator()
    {
        var profile = UserProfile.Instance;
        if (profile == null) return;

        MenuProfileName.Text = profile.DisplayName;
        AvatarRenderer.RenderSmall(MenuAvatar, MenuAvatarInitials, profile);

        var stats = StatsEngine.Calculate();
        if (stats.CurrentStreak > 0)
        {
            MenuStreakText.Text = $"\U0001f525 {stats.CurrentStreak}";

            // Pulse the streak indicator if at a milestone
            if (stats.CurrentStreak >= 5)
            {
                var pulse = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0, To = 1.3,
                    Duration = TimeSpan.FromMilliseconds(400),
                    AutoReverse = true,
                    RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(2),
                    EasingFunction = new System.Windows.Media.Animation.SineEase
                        { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                };
                MenuStreakScale.BeginAnimation(
                    System.Windows.Media.ScaleTransform.ScaleXProperty, pulse);
                MenuStreakScale.BeginAnimation(
                    System.Windows.Media.ScaleTransform.ScaleYProperty, pulse);
            }
        }
    }

    private void SetupSearchTimer()
    {
        _searchTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300) // Debounce search
        };
        _searchTimer.Tick += (s, e) =>
        {
            _searchTimer.Stop();
            PerformSearch();
        };
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer?.Stop();
        _searchTimer?.Start();
    }

    private void PerformSearch()
    {
        var keyword = SearchTextBox.Text.Trim();

        if (string.IsNullOrEmpty(keyword))
        {
            ExitSearchMode();
            return;
        }

        EnterSearchMode();

        var results = ReportStorage.SearchReports(keyword);

        if (results.Count == 0)
        {
            MonthHeader.Text = $"No results for \"{keyword}\"";
            ReportListView.ItemsSource = null;
            return;
        }

        MonthHeader.Text = $"Found {results.Count} result(s) for \"{keyword}\"";

        var items = results.Select(r => new ReportLogItem
        {
            Date = r.Date,
            DateDisplay = r.Date.ToString("dd MMM yyyy"),
            DayOfWeek = r.Date.ToString("dddd"),
            ContentPreview = r.Snippet,
            FullContent = r.Content
        }).ToList();

        ReportListView.ItemsSource = items;
    }

    private void EnterSearchMode()
    {
        ClearSearchButton.Visibility = Visibility.Visible;
        CopyMonthButton.Visibility = Visibility.Collapsed;
        DeleteMonthButton.Visibility = Visibility.Collapsed;
        MonthsListBox.SelectedItem = null;
    }

    private void ExitSearchMode()
    {
        ClearSearchButton.Visibility = Visibility.Collapsed;

        // Restore normal view
        if (MonthsListBox.Items.Count > 0 && MonthsListBox.SelectedItem == null)
        {
            MonthsListBox.SelectedIndex = 0;
        }
        else
        {
            RefreshCurrentMonth();
        }
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        ExitSearchMode();
        SearchTextBox.Focus();
    }

    private void LoadMonths()
    {
        var months = ReportStorage.GetAllMonths();

        // Always include current month even if no reports exist yet
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone);
        var currentMonth = (now.Year, now.Month);
        if (!months.Contains(currentMonth))
        {
            months.Insert(0, currentMonth);
        }

        MonthsListBox.Items.Clear();
        foreach (var (year, month) in months)
        {
            var date = new DateTime(year, month, 1);
            var item = new ListBoxItem
            {
                Content = date.ToString("MMMM yyyy"),
                Tag = (year, month)
            };
            MonthsListBox.Items.Add(item);
        }

        if (MonthsListBox.Items.Count > 0)
        {
            MonthsListBox.SelectedIndex = 0;
        }
    }

    private void MonthsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MonthsListBox.SelectedItem is not ListBoxItem item)
            return;

        var (year, month) = ((int Year, int Month))item.Tag;
        LoadReportsForMonth(year, month);
    }

    private void LoadReportsForMonth(int year, int month)
    {
        var monthDate = new DateTime(year, month, 1);
        MonthHeader.Text = monthDate.ToString("MMMM yyyy");
        DeleteMonthButton.Visibility = Visibility.Visible;
        CopyMonthButton.Visibility = Visibility.Visible;

        var reports = ReportStorage.GetReportsForMonth(year, month);
        var items = reports.Select(r => new ReportLogItem
        {
            Date = r.Date,
            DateDisplay = r.Date.ToString("dd MMM"),
            DayOfWeek = r.Date.ToString("dddd"),
            ContentPreview = r.Content.Length > 100 ? r.Content[..100] + "\u2026" : r.Content,
            FullContent = r.Content
        }).ToList();

        ReportListView.ItemsSource = items;
    }

    private void QuickAdd_Holiday_Click(object sender, RoutedEventArgs e)
    {
        AddEntryTextBox.Text = "HOLIDAY";
    }

    private void QuickAdd_SickLeave_Click(object sender, RoutedEventArgs e)
    {
        AddEntryTextBox.Text = "SICK LEAVE";
    }

    private void QuickAdd_Vacation_Click(object sender, RoutedEventArgs e)
    {
        AddEntryTextBox.Text = "VACATION";
    }

    private void SaveEntry_Click(object sender, RoutedEventArgs e)
    {
        if (AddEntryDatePicker.SelectedDate is not { } selectedDate)
        {
            MessageBox.Show("Please select a date.", "No Date", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Block future dates
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone).Date;
        if (selectedDate.Date > today)
        {
            MessageBox.Show("Cannot create reports for future dates.",
                "Future Date", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var text = AddEntryTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("Please enter report text or use a quick-add button.", "Empty Entry",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check if report already exists
        var existing = ReportStorage.LoadReport(selectedDate);
        if (existing != null)
        {
            var result = MessageBox.Show(
                $"A report already exists for {selectedDate:dd MMM yyyy}.\n\nOverwrite it?",
                "Report Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;
        }

        var tzLabel = AppSettings.GetTimeZoneAbbreviation(App.AppTimeZone, DateTime.UtcNow);
        ReportStorage.SaveReport(selectedDate, text, tzLabel);
        AddEntryTextBox.Clear();

        // Refresh and check achievements
        RefreshCurrentMonth();
        LoadProfileIndicator();
        App.CheckAchievements();

        // Make sure the month appears in the list
        var monthTag = (selectedDate.Year, selectedDate.Month);
        bool found = false;
        foreach (ListBoxItem item in MonthsListBox.Items)
        {
            if (((int Year, int Month))item.Tag == monthTag)
            {
                MonthsListBox.SelectedItem = item;
                found = true;
                break;
            }
        }

        if (!found)
        {
            LoadMonths();
        }
    }

    private void EditEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not DateTime date)
            return;

        OpenReportForEditing(date);
    }

    private void ReportListItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.ListViewItem { Content: ReportLogItem item })
        {
            OpenReportForEditing(item.Date);
            e.Handled = true;
        }
    }

    private void OpenReportForEditing(DateTime date)
    {
        var reportWindow = new ReportWindow(date);
        reportWindow.ReportSaved += (s, args) => RefreshCurrentMonth();
        reportWindow.Show();
        reportWindow.Activate();
    }

    private void CopyEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string content)
            return;

        System.Windows.Clipboard.SetText(content);
    }

    private void ReportListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
        {
            var scrollViewer = GetScrollViewer(ReportListView);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }

    private static System.Windows.Controls.ScrollViewer? GetScrollViewer(System.Windows.DependencyObject obj)
    {
        if (obj is System.Windows.Controls.ScrollViewer scrollViewer)
            return scrollViewer;

        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
            var result = GetScrollViewer(child);
            if (result != null)
                return result;
        }

        return null;
    }

    private void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not DateTime date)
            return;

        var result = MessageBox.Show(
            $"Delete the report for {date:dd MMM yyyy}?",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        ReportStorage.DeleteReport(date);
        RefreshCurrentMonth();
    }

    private void RefreshCurrentMonth()
    {
        if (MonthsListBox.SelectedItem is ListBoxItem item)
        {
            var (year, month) = ((int Year, int Month))item.Tag;
            LoadReportsForMonth(year, month);
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            // If searching, clear search first; otherwise close window
            if (!string.IsNullOrEmpty(SearchTextBox.Text))
            {
                SearchTextBox.Text = "";
                ExitSearchMode();
                e.Handled = true;
            }
            else
            {
                Close();
                e.Handled = true;
            }
        }
        else if (e.Key == System.Windows.Input.Key.F && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            Menu_Search_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.S && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            SaveEntry_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.N && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            Menu_FillReport_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.B && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            Menu_BulkEntry_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F12
            && System.Windows.Input.Keyboard.Modifiers == (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift))
        {
            if (DiagnosticsWindow.CanAccess())
            {
                var existing = System.Windows.Application.Current.Windows.OfType<DiagnosticsWindow>().FirstOrDefault();
                if (existing != null) { existing.Activate(); }
                else { var w = new DiagnosticsWindow(); w.Show(); w.Activate(); }
            }
            e.Handled = true;
        }
    }

    #region Menu Handlers

    private void Menu_FillReport_Click(object sender, RoutedEventArgs e)
    {
        if (App.Current is App app)
            app.ShowReportWindow();
    }

    private void Menu_BulkEntry_Click(object sender, RoutedEventArgs e)
    {
        var existing = System.Windows.Application.Current.Windows.OfType<BulkEntryWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        var bulkWindow = new BulkEntryWindow();
        bulkWindow.EntriesSaved += (s, args) =>
        {
            RefreshCurrentMonth();
            LoadMonths();
            LoadProfileIndicator();
            App.CheckAchievements();
        };
        bulkWindow.Show();
        bulkWindow.Activate();
    }

    private void Menu_OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        App.OpenReportsFolder();
    }

    private void Menu_Exit_Click(object sender, RoutedEventArgs e)
    {
        if (App.Current is App app)
            app.ExitApp();
    }

    private void Menu_Search_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus();
        SearchTextBox.SelectAll();
    }

    private void Menu_Settings_Click(object sender, RoutedEventArgs e)
    {
        if (App.Current is App app)
            app.ShowSettingsWindow();
    }

    private void Menu_Stats_Click(object sender, RoutedEventArgs e)
    {
        if (App.Current is App app)
            app.ShowStatsDashboard();
    }

    private void Menu_ThemeShop_Click(object sender, RoutedEventArgs e)
    {
        if (App.Current is App app)
            app.ShowThemeShop();
    }

    private void Menu_EditProfile_Click(object sender, RoutedEventArgs e)
    {
        if (App.Current is App app)
        {
            app.ShowEditProfile();
            LoadProfileIndicator();
        }
    }

    private void ProfileIndicator_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (App.Current is App app)
            app.ShowStatsDashboard();
    }

    #endregion

    #region Title Bar

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Maximize_Click(sender, e);
        }
        else if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            try { DragMove(); } catch { }
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "\u2752" : "\u25A1";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    private void DeleteMonth_Click(object sender, RoutedEventArgs e)
    {
        if (MonthsListBox.SelectedItem is not ListBoxItem item)
            return;

        var (year, month) = ((int Year, int Month))item.Tag;
        var monthDate = new DateTime(year, month, 1);

        var result = MessageBox.Show(
            $"Are you sure you want to delete ALL reports for {monthDate:MMMM yyyy}?\n\nThis action cannot be undone.",
            "Confirm Delete Month", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        var deletedCount = ReportStorage.DeleteReportsForMonth(year, month);

        MessageBox.Show($"Deleted {deletedCount} report(s) for {monthDate:MMMM yyyy}.",
            "Month Deleted", MessageBoxButton.OK, MessageBoxImage.Information);

        // Refresh the months list and clear the view
        DeleteMonthButton.Visibility = Visibility.Collapsed;
        CopyMonthButton.Visibility = Visibility.Collapsed;
        MonthHeader.Text = "Select a month to view logs";
        ReportListView.ItemsSource = null;
        LoadMonths();
    }

    private void CopyMonth_Click(object sender, RoutedEventArgs e)
    {
        if (MonthsListBox.SelectedItem is not ListBoxItem item)
            return;

        var (year, month) = ((int Year, int Month))item.Tag;
        var reports = ReportStorage.GetReportsForMonth(year, month);

        if (reports.Count == 0)
        {
            MessageBox.Show("No reports to copy for this month.", "Empty Month", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var monthDate = new DateTime(year, month, 1);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ETS Reports - {monthDate:MMMM yyyy}");
        sb.AppendLine(new string('=', 40));
        sb.AppendLine();

        foreach (var (date, content) in reports)
        {
            sb.AppendLine($"Date: {date:dddd, dd MMMM yyyy}");
            sb.AppendLine(new string('-', 30));
            sb.AppendLine(content);
            sb.AppendLine();
        }

        System.Windows.Clipboard.SetText(sb.ToString());
        MessageBox.Show($"Copied {reports.Count} report(s) for {monthDate:MMMM yyyy} to clipboard.",
            "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
