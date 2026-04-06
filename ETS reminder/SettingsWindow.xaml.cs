using System.Windows;

namespace ETS_reminder;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = AppSettings.Instance;
        DarkModeCheckBox.IsChecked = settings.DarkMode;
        AutoSaveDraftCheckBox.IsChecked = settings.AutoSaveDraft;
        ShowLogViewerOnStartupCheckBox.IsChecked = settings.ShowLogViewerOnStartup;
        AutoSaveIntervalTextBox.Text = settings.AutoSaveIntervalSeconds.ToString();

        // Populate timezone list with all system time zones
        var allZones = TimeZoneInfo.GetSystemTimeZones()
            .OrderBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.DisplayName)
            .ToList();

        foreach (var tz in allZones)
            TimeZoneComboBox.Items.Add(tz.DisplayName);

        // Select the saved timezone
        var savedTz = settings.GetTimeZone();
        var selectedIndex = allZones.FindIndex(tz => tz.Id == savedTz.Id);
        TimeZoneComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

        // Populate hour (0-23) and minute (00, 05, 10, ..., 55) combo boxes
        for (int h = 0; h < 24; h++)
            ReminderHourComboBox.Items.Add(h.ToString("D2"));
        for (int m = 0; m < 60; m += 5)
            ReminderMinuteComboBox.Items.Add(m.ToString("D2"));

        ReminderHourComboBox.SelectedIndex = settings.ReminderHour;

        // Select the closest 5-minute slot
        var minuteIndex = settings.ReminderMinute / 5;
        ReminderMinuteComboBox.SelectedIndex = Math.Clamp(minuteIndex, 0, 11);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = AppSettings.Instance;
        settings.DarkMode = DarkModeCheckBox.IsChecked ?? false;
        settings.AutoSaveDraft = AutoSaveDraftCheckBox.IsChecked ?? true;
        settings.ShowLogViewerOnStartup = ShowLogViewerOnStartupCheckBox.IsChecked ?? true;

        if (int.TryParse(AutoSaveIntervalTextBox.Text, out int interval) && interval > 0)
        {
            settings.AutoSaveIntervalSeconds = interval;
        }

        // Save selected timezone
        var allZones = TimeZoneInfo.GetSystemTimeZones()
            .OrderBy(tz => tz.BaseUtcOffset)
            .ThenBy(tz => tz.DisplayName)
            .ToList();

        if (TimeZoneComboBox.SelectedIndex >= 0 && TimeZoneComboBox.SelectedIndex < allZones.Count)
        {
            settings.TimeZoneId = allZones[TimeZoneComboBox.SelectedIndex].Id;
        }

        if (ReminderHourComboBox.SelectedIndex >= 0)
            settings.ReminderHour = ReminderHourComboBox.SelectedIndex;
        if (ReminderMinuteComboBox.SelectedIndex >= 0)
            settings.ReminderMinute = ReminderMinuteComboBox.SelectedIndex * 5;

        settings.Save();

        // Apply theme immediately
        ThemeManager.ApplyTheme(settings.DarkMode);

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
