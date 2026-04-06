namespace ETS_reminder;

public record ReportLogItem
{
    public DateTime Date { get; init; }
    public string DateDisplay { get; init; } = "";
    public string DayOfWeek { get; init; } = "";
    public string ContentPreview { get; init; } = "";
    public string FullContent { get; init; } = "";
}
