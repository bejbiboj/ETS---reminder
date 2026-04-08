using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace ETS_reminder;

public partial class BulkEntryWindow : Window
{
    public event EventHandler? EntriesSaved;

    public BulkEntryWindow()
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();

        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone).Date;
        StartDatePicker.SelectedDate = today.AddDays(-6);
        EndDatePicker.SelectedDate = today;
    }

    private void EntryTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomEntryTextBox == null) return;

        var selected = (EntryTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (selected == "Custom...")
        {
            CustomEntryTextBox.Visibility = Visibility.Visible;
            CustomEntryTextBox.Focus();
        }
        else
        {
            CustomEntryTextBox.Visibility = Visibility.Collapsed;
        }
    }

    private string GetEntryText()
    {
        var selected = (EntryTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (selected == "Custom...")
            return CustomEntryTextBox.Text.Trim();
        return selected ?? "";
    }

    private List<DateTime> GetTargetDates()
    {
        if (StartDatePicker.SelectedDate is not { } start || EndDatePicker.SelectedDate is not { } end)
            return [];

        if (start > end)
            return [];

        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone).Date;
        if (end > today)
            end = today;

        var weekdaysOnly = WeekdaysOnlyCheckBox.IsChecked == true;
        var skipExisting = SkipExistingCheckBox.IsChecked == true;

        var dates = new List<DateTime>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (weekdaysOnly && date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            if (skipExisting && ReportStorage.LoadReport(date) != null)
                continue;

            dates.Add(date);
        }

        return dates;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var text = GetEntryText();
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("Please select an entry type or enter custom text.",
                "Empty Content", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
        {
            MessageBox.Show("Please select both a start and end date.",
                "Missing Dates", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
        {
            MessageBox.Show("Start date must be before or equal to end date.",
                "Invalid Range", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dates = GetTargetDates();

        if (dates.Count == 0)
        {
            MessageBox.Show("No dates to fill. All dates in the range already have reports or were filtered out.",
                "Nothing to Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"This will create \"{text}\" entries for {dates.Count} day(s):\n\n" +
            $"{dates[0]:dd MMM yyyy} \u2192 {dates[^1]:dd MMM yyyy}\n\n" +
            "Continue?",
            "Confirm Bulk Entry", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        var tzLabel = AppSettings.GetTimeZoneAbbreviation(App.AppTimeZone, DateTime.UtcNow);

        foreach (var date in dates)
        {
            ReportStorage.SaveReport(date, text, tzLabel);
        }

        EntriesSaved?.Invoke(this, EventArgs.Empty);

        MessageBox.Show($"Done! Created {dates.Count} report(s).",
            "Bulk Entry Complete", MessageBoxButton.OK, MessageBoxImage.Information);

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
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
