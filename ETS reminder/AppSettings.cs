using System.IO;
using System.Text.Json;

namespace ETS_reminder;

public class AppSettings
{
    public bool AutoSaveDraft { get; set; } = true;
    public bool ShowLogViewerOnStartup { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 30;
    public int ReminderHour { get; set; } = 12;
    public int ReminderMinute { get; set; } = 0;
    public string TimeZoneId { get; set; } = "Central European Standard Time";
    public bool DarkMode { get; set; } = false;
    public string ActiveThemeId { get; set; } = "default_light";

    public TimeZoneInfo GetTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Local;
        }
    }

    public static string GetTimeZoneAbbreviation(TimeZoneInfo tz, DateTime utcNow)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        return tz.IsDaylightSavingTime(local) ? tz.DaylightName : tz.StandardName;
    }

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ETS Reminder",
        "settings.json");

    private static AppSettings? _instance;

    public static AppSettings Instance => _instance ??= Load();

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If loading fails, return defaults
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var folder = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }
}
