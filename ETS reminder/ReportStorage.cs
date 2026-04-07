using System.IO;

namespace ETS_reminder;

public static class ReportStorage
{
    // Leave-type keywords that preserve streak but earn 0 coins
    private static readonly string[] LeaveKeywords = ["HOLIDAY", "SICK LEAVE", "VACATION"];

    public static string GetReportsFolder()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents, "ETS Reports");
    }

    public static string SaveReport(DateTime date, string content, string? timeZoneLabel = null)
    {
        var folder = GetReportsFolder();
        Directory.CreateDirectory(folder);

        var fileName = $"ETS_{date:yyyy-MM-dd}.txt";
        var filePath = Path.Combine(folder, fileName);

        var tzLabel = timeZoneLabel ?? AppSettings.GetTimeZoneAbbreviation(AppSettings.Instance.GetTimeZone(), DateTime.UtcNow);
        var fullContent = $"""
ETS Daily Report
=================
Date: {date:dddd, MMMM dd, yyyy}
Time: {date:HH:mm} {tzLabel}

Report:
-------
{content}
""";

        File.WriteAllText(filePath, fullContent);

        // Save metadata (creation timestamp) for coin eligibility
        SaveMetadata(date);

        return filePath;
    }

    /// <summary>
    /// Returns true if the report content is a leave entry (holiday, sick leave, vacation).
    /// </summary>
    public static bool IsLeaveEntry(string content)
    {
        var trimmed = content.Trim().ToUpperInvariant();
        return LeaveKeywords.Any(k => trimmed == k);
    }

    /// <summary>
    /// Returns true if the report for the given date was entered on the same day (not backdated).
    /// </summary>
    public static bool WasEnteredOnTime(DateTime reportDate)
    {
        var metaPath = GetMetadataPath(reportDate);
        if (!File.Exists(metaPath)) return true; // Legacy reports without metadata get benefit of the doubt

        try
        {
            var createdDateStr = File.ReadAllText(metaPath).Trim();
            if (DateOnly.TryParse(createdDateStr, out var createdDate))
            {
                return createdDate == DateOnly.FromDateTime(reportDate);
            }
        }
        catch { }
        return true; // If metadata is corrupt, give benefit of the doubt
    }

    private static string GetMetadataPath(DateTime date)
    {
        var folder = GetReportsFolder();
        return Path.Combine(folder, $"ETS_{date:yyyy-MM-dd}.meta");
    }

    private static void SaveMetadata(DateTime reportDate)
    {
        try
        {
            var today = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppSettings.Instance.GetTimeZone()));
            var metaPath = GetMetadataPath(reportDate);

            // Only write metadata if it doesn't exist yet (preserve original entry date)
            if (!File.Exists(metaPath))
            {
                File.WriteAllText(metaPath, today.ToString("yyyy-MM-dd"));
            }
        }
        catch { }
    }

    public static string? LoadReport(DateTime date)
    {
        var folder = GetReportsFolder();
        var fileName = $"ETS_{date:yyyy-MM-dd}.txt";
        var filePath = Path.Combine(folder, fileName);

        if (!File.Exists(filePath))
            return null;

        var text = File.ReadAllText(filePath);

        // Extract just the report content after "Report:\n-------\n"
        var marker = "-------";
        var idx = text.IndexOf(marker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            return text[(idx + marker.Length)..].Trim();
        }

        return text;
    }

    public static List<DateTime> GetAllReportDates()
    {
        var folder = GetReportsFolder();
        if (!Directory.Exists(folder))
            return [];

        var dates = new List<DateTime>();
        foreach (var file in Directory.GetFiles(folder, "ETS_*.txt"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            // Format: ETS_yyyy-MM-dd
            if (name.Length >= 14 && DateTime.TryParseExact(name[4..],
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var date))
            {
                dates.Add(date);
            }
        }

        dates.Sort();
        return dates;
    }

    public static List<(int Year, int Month)> GetAllMonths()
    {
        var dates = GetAllReportDates();
        return dates
            .Select(d => (d.Year, d.Month))
            .Distinct()
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();
    }

    public static List<(DateTime Date, string Content)> GetReportsForMonth(int year, int month)
    {
        var dates = GetAllReportDates()
            .Where(d => d.Year == year && d.Month == month)
            .OrderBy(d => d)
            .ToList();

        var results = new List<(DateTime Date, string Content)>();
        foreach (var date in dates)
        {
            var content = LoadReport(date) ?? "";
            results.Add((date, content));
        }

        return results;
    }

    public static void DeleteReport(DateTime date)
    {
        var folder = GetReportsFolder();
        var fileName = $"ETS_{date:yyyy-MM-dd}.txt";
        var filePath = Path.Combine(folder, fileName);

        if (File.Exists(filePath))
            File.Delete(filePath);

        // Clean up metadata
        var metaPath = GetMetadataPath(date);
        if (File.Exists(metaPath))
            File.Delete(metaPath);
    }

    public static int DeleteReportsForMonth(int year, int month)
    {
        var reports = GetReportsForMonth(year, month);
        int deletedCount = 0;

        foreach (var (date, _) in reports)
        {
            DeleteReport(date);
            deletedCount++;
        }

        return deletedCount;
    }

    // Search functionality
    public static List<(DateTime Date, string Content, string Snippet)> SearchReports(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        var results = new List<(DateTime Date, string Content, string Snippet)>();
        var allDates = GetAllReportDates();
        var keywordLower = keyword.ToLowerInvariant();

        foreach (var date in allDates.OrderByDescending(d => d))
        {
            var content = LoadReport(date);
            if (string.IsNullOrEmpty(content))
                continue;

            var contentLower = content.ToLowerInvariant();
            var index = contentLower.IndexOf(keywordLower, StringComparison.Ordinal);

            if (index >= 0)
            {
                // Create snippet with context around the keyword
                var snippetStart = Math.Max(0, index - 30);
                var snippetEnd = Math.Min(content.Length, index + keyword.Length + 50);
                var snippet = content[snippetStart..snippetEnd].Trim();

                if (snippetStart > 0) snippet = "..." + snippet;
                if (snippetEnd < content.Length) snippet += "...";

                results.Add((date, content, snippet));
            }
        }

        return results;
    }

    // Draft functionality
    private static string GetDraftPath(DateTime date)
    {
        var folder = GetReportsFolder();
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"draft_{date:yyyy-MM-dd}.tmp");
    }

    public static void SaveDraft(DateTime date, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        try
        {
            var draftPath = GetDraftPath(date);
            File.WriteAllText(draftPath, content);
        }
        catch
        {
            // Silently fail - draft saving is not critical
        }
    }

    public static string? LoadDraft(DateTime date)
    {
        try
        {
            var draftPath = GetDraftPath(date);
            if (File.Exists(draftPath))
            {
                return File.ReadAllText(draftPath);
            }
        }
        catch
        {
            // Silently fail
        }
        return null;
    }

    public static void DeleteDraft(DateTime date)
    {
        try
        {
            var draftPath = GetDraftPath(date);
            if (File.Exists(draftPath))
            {
                File.Delete(draftPath);
            }
        }
        catch
        {
            // Silently fail
        }
    }

    // RTF storage (companion files for rich text formatting)

    private static string GetRtfPath(DateTime date)
    {
        var folder = GetReportsFolder();
        return Path.Combine(folder, $"ETS_{date:yyyy-MM-dd}.rtf");
    }

    private static string GetDraftRtfPath(DateTime date)
    {
        var folder = GetReportsFolder();
        return Path.Combine(folder, $"draft_{date:yyyy-MM-dd}.rtf");
    }

    public static void SaveReportRtf(DateTime date, byte[] rtfBytes)
    {
        try
        {
            var folder = GetReportsFolder();
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(GetRtfPath(date), rtfBytes);
        }
        catch
        {
            // Silently fail - RTF is a nice-to-have
        }
    }

    public static byte[]? LoadReportRtf(DateTime date)
    {
        try
        {
            var path = GetRtfPath(date);
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }
        catch { }
        return null;
    }

    public static void SaveDraftRtf(DateTime date, byte[] rtfBytes)
    {
        try
        {
            var folder = GetReportsFolder();
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(GetDraftRtfPath(date), rtfBytes);
        }
        catch { }
    }

    public static byte[]? LoadDraftRtf(DateTime date)
    {
        try
        {
            var path = GetDraftRtfPath(date);
            if (File.Exists(path))
                return File.ReadAllBytes(path);
        }
        catch { }
        return null;
    }

    public static void DeleteDraftRtf(DateTime date)
    {
        try
        {
            var path = GetDraftRtfPath(date);
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }
}
