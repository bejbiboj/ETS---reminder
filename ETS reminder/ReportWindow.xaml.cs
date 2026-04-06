using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;
using FontStyle = System.Windows.FontStyle;

namespace ETS_reminder;

public partial class ReportWindow : Window
{
    private readonly DateTime _reportDate;
    private System.Windows.Threading.DispatcherTimer? _autoSaveTimer;
    private bool _isDirty;
    private bool _suppressToolbarUpdate;
    private bool _isInitializing = true;
    private static readonly double[] FontSizes = [10, 11, 12, 14, 16, 18, 20, 24, 28, 32];

    public event EventHandler? ReportSaved;

    public ReportWindow(DateTime reportDate)
    {
        InitializeComponent();
        Icon = App.GetWindowIcon();
        _reportDate = reportDate;
        var tzName = AppSettings.GetTimeZoneAbbreviation(App.AppTimeZone, DateTime.UtcNow);
        DateLabel.Text = $"Date: {_reportDate:dddd, MMMM dd, yyyy} \u2014 {_reportDate:HH:mm} {tzName}";

        // Populate font size combo
        _suppressToolbarUpdate = true;
        foreach (var size in FontSizes)
            FontSizeComboBox.Items.Add(size);
        FontSizeComboBox.SelectedItem = 14.0;
        _suppressToolbarUpdate = false;

        // Load existing report, or recover draft if available
        var existingRtf = ReportStorage.LoadReportRtf(_reportDate);
        var existing = ReportStorage.LoadReport(_reportDate);
        var draftRtf = ReportStorage.LoadDraftRtf(_reportDate);
        var draft = ReportStorage.LoadDraft(_reportDate);

        if (existingRtf != null)
        {
            LoadRtf(existingRtf);
        }
        else if (!string.IsNullOrEmpty(existing))
        {
            SetPlainText(existing);
        }
        else if (draftRtf != null)
        {
            LoadRtf(draftRtf);
            StatusBar.Text = "Draft recovered | Words: 0 | Characters: 0";
        }
        else if (!string.IsNullOrEmpty(draft))
        {
            SetPlainText(draft);
            StatusBar.Text = "Draft recovered | Words: 0 | Characters: 0";
        }

        _isInitializing = false;
        SetupAutoSave();
        UpdateStatusBar();
        ReportRichTextBox.Focus();
    }

    #region RTF Helpers

    private string GetPlainText()
    {
        var range = new TextRange(ReportRichTextBox.Document.ContentStart, ReportRichTextBox.Document.ContentEnd);
        return range.Text.TrimEnd();
    }

    private void SetPlainText(string text)
    {
        ReportRichTextBox.Document.Blocks.Clear();
        ReportRichTextBox.Document.Blocks.Add(new Paragraph(new Run(text)));
    }

    private byte[] GetRtfBytes()
    {
        var range = new TextRange(ReportRichTextBox.Document.ContentStart, ReportRichTextBox.Document.ContentEnd);
        using var ms = new System.IO.MemoryStream();
        range.Save(ms, System.Windows.DataFormats.Rtf);
        return ms.ToArray();
    }

    private void LoadRtf(byte[] rtfBytes)
    {
        var range = new TextRange(ReportRichTextBox.Document.ContentStart, ReportRichTextBox.Document.ContentEnd);
        using var ms = new System.IO.MemoryStream(rtfBytes);
        range.Load(ms, System.Windows.DataFormats.Rtf);
    }

    #endregion

    #region Formatting Toolbar

    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        var prop = TextElement.FontWeightProperty;
        var current = ReportRichTextBox.Selection.GetPropertyValue(prop);
        var newWeight = (current is FontWeight fw && fw == FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold;
        ReportRichTextBox.Selection.ApplyPropertyValue(prop, newWeight);
        ReportRichTextBox.Focus();
    }

    private void Italic_Click(object sender, RoutedEventArgs e)
    {
        var prop = TextElement.FontStyleProperty;
        var current = ReportRichTextBox.Selection.GetPropertyValue(prop);
        var newStyle = (current is FontStyle fs && fs == FontStyles.Italic) ? FontStyles.Normal : FontStyles.Italic;
        ReportRichTextBox.Selection.ApplyPropertyValue(prop, newStyle);
        ReportRichTextBox.Focus();
    }

    private void Underline_Click(object sender, RoutedEventArgs e)
    {
        var prop = Inline.TextDecorationsProperty;
        var current = ReportRichTextBox.Selection.GetPropertyValue(prop);
        var newDeco = (current is TextDecorationCollection td && td.Count > 0) ? new TextDecorationCollection() : TextDecorations.Underline;
        ReportRichTextBox.Selection.ApplyPropertyValue(prop, newDeco);
        ReportRichTextBox.Focus();
    }

    private void BulletList_Click(object sender, RoutedEventArgs e)
    {
        EditingCommands.ToggleBullets.Execute(null, ReportRichTextBox);
        ReportRichTextBox.Focus();
    }

    private void FontSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressToolbarUpdate || FontSizeComboBox.SelectedItem == null) return;

        var size = (double)FontSizeComboBox.SelectedItem;
        ReportRichTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
        ReportRichTextBox.Focus();
    }

    private void ReportRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        UpdateToolbarState();
    }

    private void UpdateToolbarState()
    {
        _suppressToolbarUpdate = true;
        try
        {
            // Bold
            var weight = ReportRichTextBox.Selection.GetPropertyValue(TextElement.FontWeightProperty);
            BoldButton.IsChecked = weight is FontWeight fw && fw == FontWeights.Bold;

            // Italic
            var style = ReportRichTextBox.Selection.GetPropertyValue(TextElement.FontStyleProperty);
            ItalicButton.IsChecked = style is FontStyle fs && fs == FontStyles.Italic;

            // Underline
            var deco = ReportRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            UnderlineButton.IsChecked = deco is TextDecorationCollection td && td.Count > 0;

            // Font size
            var fontSize = ReportRichTextBox.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            if (fontSize is double sz)
            {
                FontSizeComboBox.SelectedItem = FontSizes.FirstOrDefault(s => Math.Abs(s - sz) < 0.5);
            }
        }
        finally
        {
            _suppressToolbarUpdate = false;
        }
    }

    #endregion

    private void SetupAutoSave()
    {
        if (!AppSettings.Instance.AutoSaveDraft)
            return;

        _autoSaveTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(AppSettings.Instance.AutoSaveIntervalSeconds)
        };
        _autoSaveTimer.Tick += (s, e) =>
        {
            if (_isDirty)
            {
                ReportStorage.SaveDraft(_reportDate, GetPlainText());
                ReportStorage.SaveDraftRtf(_reportDate, GetRtfBytes());
                _isDirty = false;
            }
        };
        _autoSaveTimer.Start();
    }

    private void ReportRichTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        _isDirty = true;
        UpdateStatusBar();
    }

    private void UpdateStatusBar()
    {
        var text = GetPlainText();
        var charCount = text.Length;
        var wordCount = string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;

        var autoSaveStatus = AppSettings.Instance.AutoSaveDraft ? " | Auto-save: ON" : "";
        StatusBar.Text = $"Words: {wordCount} | Characters: {charCount}{autoSaveStatus}";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var text = GetPlainText().Trim();
        if (string.IsNullOrEmpty(text))
        {
            System.Windows.MessageBox.Show("Please enter your ETS report before saving.",
                "Empty Report", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tzLabel = AppSettings.GetTimeZoneAbbreviation(App.AppTimeZone, DateTime.UtcNow);
        var filePath = ReportStorage.SaveReport(_reportDate, text, tzLabel);
        ReportStorage.SaveReportRtf(_reportDate, GetRtfBytes());
        ReportStorage.DeleteDraft(_reportDate);
        ReportStorage.DeleteDraftRtf(_reportDate);
        ReportSaved?.Invoke(this, EventArgs.Empty);

        System.Windows.MessageBox.Show($"Report saved!\n\n{filePath}",
            "ETS Report Saved", MessageBoxButton.OK, MessageBoxImage.Information);

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer = null;
        base.OnClosed(e);
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.S && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            SaveButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            ShowFindBar();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            if (FindBar.Visibility == Visibility.Visible)
            {
                CloseFindBar();
                e.Handled = true;
            }
            else
            {
                Close();
                e.Handled = true;
            }
        }
        else if (e.Key == System.Windows.Input.Key.L && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            ViewLogsButton_Click(sender, e);
            e.Handled = true;
        }
    }

    #region Find Functionality

    private TextPointer? _lastFindPosition;

    private void ShowFindBar()
    {
        FindBar.Visibility = Visibility.Visible;
        FindTextBox.Focus();
        FindTextBox.SelectAll();
    }

    private void CloseFindBar()
    {
        FindBar.Visibility = Visibility.Collapsed;
        _lastFindPosition = null;
        FindResultsLabel.Text = "";
        ReportRichTextBox.Focus();
    }

    private void CloseFindBar_Click(object sender, RoutedEventArgs e) => CloseFindBar();

    private void FindTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _lastFindPosition = null;
        FindNext();
    }

    private void FindTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
                FindPrevious();
            else
                FindNext();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            CloseFindBar();
            e.Handled = true;
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        FindNext();
        FindTextBox.Focus();
    }

    private void FindPrevious_Click(object sender, RoutedEventArgs e)
    {
        FindPrevious();
        FindTextBox.Focus();
    }

    private void FindNext()
    {
        var searchText = FindTextBox.Text;
        if (string.IsNullOrEmpty(searchText))
        {
            FindResultsLabel.Text = "";
            return;
        }

        var start = _lastFindPosition ?? ReportRichTextBox.Document.ContentStart;
        var result = FindTextInRange(start, ReportRichTextBox.Document.ContentEnd, searchText);

        if (result == null)
        {
            // Wrap around from the beginning
            result = FindTextInRange(ReportRichTextBox.Document.ContentStart, ReportRichTextBox.Document.ContentEnd, searchText);
        }

        if (result != null)
        {
            ReportRichTextBox.Selection.Select(result.Start, result.End);
            ReportRichTextBox.Focus();
            _lastFindPosition = result.End;
            FindResultsLabel.Text = "Found";

            Dispatcher.BeginInvoke(new Action(() =>
            {
                FindTextBox.Focus();
                FindTextBox.CaretIndex = FindTextBox.Text.Length;
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
        else
        {
            FindResultsLabel.Text = "No results";
        }
    }

    private void FindPrevious()
    {
        var searchText = FindTextBox.Text;
        if (string.IsNullOrEmpty(searchText))
        {
            FindResultsLabel.Text = "";
            return;
        }

        // Simple approach: find all, pick the one before current
        var content = GetPlainText();
        var searchLower = searchText.ToLowerInvariant();
        var contentLower = content.ToLowerInvariant();

        var matches = new List<int>();
        int idx = 0;
        while ((idx = contentLower.IndexOf(searchLower, idx, StringComparison.Ordinal)) != -1)
        {
            matches.Add(idx);
            idx += searchText.Length;
        }

        if (matches.Count == 0)
        {
            FindResultsLabel.Text = "No results";
            return;
        }

        // Find position before current selection
        var currentOffset = GetTextOffset(ReportRichTextBox.Selection.Start);
        var prevMatch = matches.LastOrDefault(m => m < currentOffset, matches[^1]);

        SelectTextByOffset(prevMatch, searchText.Length);
        FindResultsLabel.Text = "Found";
    }

    private static TextRange? FindTextInRange(TextPointer start, TextPointer end, string searchText)
    {
        var current = start;
        while (current != null && current.CompareTo(end) < 0)
        {
            if (current.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var textRun = current.GetTextInRun(LogicalDirection.Forward);
                var index = textRun.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase);
                if (index >= 0)
                {
                    var matchStart = current.GetPositionAtOffset(index);
                    var matchEnd = matchStart?.GetPositionAtOffset(searchText.Length);
                    if (matchStart != null && matchEnd != null)
                        return new TextRange(matchStart, matchEnd);
                }
            }
            current = current.GetNextContextPosition(LogicalDirection.Forward);
        }
        return null;
    }

    private int GetTextOffset(TextPointer pointer)
    {
        return new TextRange(ReportRichTextBox.Document.ContentStart, pointer).Text.Length;
    }

    private void SelectTextByOffset(int offset, int length)
    {
        var start = GetPointerAtOffset(offset);
        var end = start?.GetPositionAtOffset(length);
        if (start != null && end != null)
        {
            ReportRichTextBox.Selection.Select(start, end);
            ReportRichTextBox.Focus();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                FindTextBox.Focus();
                FindTextBox.CaretIndex = FindTextBox.Text.Length;
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private TextPointer? GetPointerAtOffset(int offset)
    {
        var pointer = ReportRichTextBox.Document.ContentStart;
        int remaining = offset;
        while (pointer != null && remaining > 0)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var runLength = pointer.GetTextRunLength(LogicalDirection.Forward);
                if (runLength >= remaining)
                    return pointer.GetPositionAtOffset(remaining);
                remaining -= runLength;
            }
            pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
        }
        return pointer;
    }

    #endregion

    private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
    {
        var existing = System.Windows.Application.Current.Windows.OfType<LogViewerWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        var logViewer = new LogViewerWindow();
        logViewer.Show();
        logViewer.Activate();
    }
}
