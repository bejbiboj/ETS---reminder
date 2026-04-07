using System.IO;
using System.Text.Json;

namespace ETS_reminder;

public class UserProfile
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string AvatarColor { get; set; } = "#E67E22";
    public string AuthProvider { get; set; } = "Local";
    public int TotalCoins { get; set; }
    public int BonusCoins { get; set; }
    public int LongestStreak { get; set; }
    public List<string> UnlockedAchievements { get; set; } = [];
    public List<string> UnlockedThemes { get; set; } = ["default_dark", "default_light"];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool SpendCoins(int amount)
    {
        if (TotalCoins < amount) return false;
        // Deduct from BonusCoins since earned coins are recalculated by StatsEngine
        BonusCoins = Math.Max(0, BonusCoins - amount);
        TotalCoins -= amount;
        Save();
        return true;
    }

    public string Initials
    {
        get
        {
            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpper(),
                _ => $"{parts[0][..1]}{parts[^1][..1]}".ToUpper()
            };
        }
    }

    public int Level => TotalCoins switch
    {
        < 10 => 1,
        < 30 => 2,
        < 60 => 3,
        < 100 => 4,
        < 150 => 5,
        < 220 => 6,
        < 300 => 7,
        < 400 => 8,
        < 500 => 9,
        _ => 10
    };

    public string LevelTitle => Level switch
    {
        1 => "Newcomer",
        2 => "Reporter",
        3 => "Consistent",
        4 => "Dedicated",
        5 => "Reliable",
        6 => "Veteran",
        7 => "Champion",
        8 => "Legend",
        9 => "Master",
        10 => "ETS God",
        _ => "Unknown"
    };

    public int CoinsForNextLevel => Level switch
    {
        1 => 10,
        2 => 30,
        3 => 60,
        4 => 100,
        5 => 150,
        6 => 220,
        7 => 300,
        8 => 400,
        9 => 500,
        _ => 999
    };

    private static readonly string ProfilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ETS Reminder",
        "profile.json");

    private static UserProfile? _instance;

    public static UserProfile? Instance => _instance ??= Load();

    public static bool Exists => File.Exists(ProfilePath);

    private static UserProfile? Load()
    {
        try
        {
            if (File.Exists(ProfilePath))
            {
                var json = File.ReadAllText(ProfilePath);
                return JsonSerializer.Deserialize<UserProfile>(json);
            }
        }
        catch { }
        return null;
    }

    public void Save()
    {
        try
        {
            var folder = Path.GetDirectoryName(ProfilePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfilePath, json);
            _instance = this;
        }
        catch { }
    }

    public static void Reload()
    {
        _instance = null;
    }
}
