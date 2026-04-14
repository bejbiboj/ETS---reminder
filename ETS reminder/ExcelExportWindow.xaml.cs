using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using ClosedXML.Excel;
using MessageBox = System.Windows.MessageBox;

namespace ETS_reminder;

public partial class ExcelExportWindow : Window
{
    public ExcelExportWindow()
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
    }

    private void GenerateExcel_Click(object sender, RoutedEventArgs e)
    {
        var text = PasteTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            MessageBox.Show("Please paste the Copilot output first.",
                "Empty", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var entries = ParseCopilotOutput(text);
        if (entries.Count == 0)
        {
            MessageBox.Show(
                "Could not parse any entries.\n\n" +
                "Expected format:\n" +
                "[Apr 01 2026 | Block 1]\n" +
                "First 4-hour block description here.\n\n" +
                "[Apr 01 2026 | Block 2]\n" +
                "Second 4-hour block description here.",
                "Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var (filePath, rowCount) = GenerateExcelFile(entries);
            StatusText.Text = $"Exported {rowCount} row(s) to Excel.";

            var result = MessageBox.Show(
                $"Excel file saved with {rowCount} row(s)!\n\n{filePath}\n\nOpen the file now?",
                "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating Excel file:\n\n{ex.Message}",
                "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static List<ExcelEntry> ParseCopilotOutput(string text)
    {
        var entries = new List<ExcelEntry>();

        // Match headers like [Apr 01 2026 | Block 1], [Apr 01 2026 | Block 2]
        var headerPattern = new Regex(
            @"\[(\w{3}\s+\d{1,2}\s+\d{4})\s*\|\s*(.+?)\]",
            RegexOptions.IgnoreCase);

        var lines = text.Split('\n');
        ExcelEntry? current = null;
        var descriptionLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            var match = headerPattern.Match(line);

            if (match.Success)
            {
                // Save previous entry
                if (current != null)
                {
                    current.Description = CleanDescription(string.Join(" ", descriptionLines).Trim());
                    if (!string.IsNullOrEmpty(current.Description))
                        entries.Add(current);
                }

                var dateStr = match.Groups[1].Value;
                var blockType = match.Groups[2].Value.Trim();

                if (!TryParseDate(dateStr, out var date))
                {
                    current = null;
                    descriptionLines.Clear();
                    continue;
                }

                current = new ExcelEntry { Date = date };
                descriptionLines.Clear();

                current.ProjectTask = "MS Azure Dedicated.Development";
            }
            else if (current != null && !string.IsNullOrWhiteSpace(line))
            {
                descriptionLines.Add(line);
            }
        }

        // Don't forget the last entry
        if (current != null)
        {
            current.Description = CleanDescription(string.Join(" ", descriptionLines).Trim());
            if (!string.IsNullOrEmpty(current.Description))
                entries.Add(current);
        }

        return entries;
    }

    private static string CleanDescription(string text)
    {
        // Strip markdown artifacts Copilot may leave behind
        text = text.Replace("*", "").Replace("#", "");
        // Collapse multiple spaces into one
        while (text.Contains("  "))
            text = text.Replace("  ", " ");
        return text.Trim();
    }

    private static bool TryParseDate(string dateStr, out DateTime date)
    {
        // Handle formats like "Apr 01 2026", "Apr 1 2026"
        var formats = new[] { "MMM dd yyyy", "MMM d yyyy" };
        return DateTime.TryParseExact(dateStr, formats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static (string FilePath, int RowCount) GenerateExcelFile(List<ExcelEntry> entries)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");

        // Row 1: Template name
        ws.Cell(1, 1).Value = "ETSI_OneDayTaskReportTemplate";
        ws.Range(1, 1, 1, 4).Merge();
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Row 2: Headers
        ws.Cell(2, 1).Value = "Project-Task";
        ws.Cell(2, 2).Value = "Effort";
        ws.Cell(2, 3).Value = "Description";
        ws.Cell(2, 4).Value = "Date";

        // Style headers
        var headerRange = ws.Range(2, 1, 2, 4);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#000080");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Row 3+: Data
        var row = 3;
        foreach (var entry in entries)
        {
            ws.Cell(row, 1).Value = entry.ProjectTask;
            ws.Cell(row, 2).Value = 4;
            ws.Cell(row, 3).Value = entry.Description;
            ws.Cell(row, 4).Value = entry.Date;
            ws.Cell(row, 4).Style.DateFormat.Format = "MM/dd/yyyy";
            row++;
        }

        // Auto-fit columns
        ws.Column(1).Width = 35;
        ws.Column(2).Width = 8;
        ws.Column(3).Width = 80;
        ws.Column(4).Width = 15;

        // Save
        var folder = ReportStorage.GetReportsFolder();
        Directory.CreateDirectory(folder);
        var fileName = $"ETS_Export_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx";
        var filePath = Path.Combine(folder, fileName);
        workbook.SaveAs(filePath);

        return (filePath, entries.Count);
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

public class ExcelEntry
{
    public DateTime Date { get; set; }
    public string ProjectTask { get; set; } = "";
    public string Description { get; set; } = "";
}
