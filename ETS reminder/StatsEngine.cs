namespace ETS_reminder;

public class StatsResult
{
    public int TotalReports { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
    public int TotalCoins { get; init; }
    public int CoinsEarnedToday { get; init; }
    public double CompletionRatePercent { get; init; }
    public DateOnly? FirstReportDate { get; init; }
    public DateOnly? LastReportDate { get; init; }
}

public static class StatsEngine
{
    public static StatsResult Calculate()
    {
        var allDates = ReportStorage.GetAllReportDates()
            .Select(DateOnly.FromDateTime)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (allDates.Count == 0)
        {
            return new StatsResult();
        }

        var totalReports = allDates.Count;
        var firstReport = allDates[0];
        var lastReport = allDates[^1];

        // Calculate current streak (consecutive weekdays from today going back)
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone));
        var reportSet = allDates.ToHashSet();
        var currentStreak = CalculateCurrentStreak(today, reportSet);

        // Calculate longest streak ever
        var longestStreak = CalculateLongestStreak(allDates);

        // Calculate coins: 1 per report + streak bonuses
        var totalCoins = CalculateCoins(allDates);

        // Today's coins
        var todayCoins = reportSet.Contains(today) ? CalculateStreakBonus(currentStreak) + 1 : 0;

        // Completion rate: reports filed vs weekdays since first report
        var totalWeekdays = CountWeekdays(firstReport, today);
        var completionRate = totalWeekdays > 0 ? (double)totalReports / totalWeekdays * 100 : 0;

        // Update profile with persistent stats
        var profile = UserProfile.Instance;
        if (profile != null)
        {
            if (longestStreak > profile.LongestStreak)
                profile.LongestStreak = longestStreak;
            profile.TotalCoins = totalCoins;
            profile.Save();
        }

        return new StatsResult
        {
            TotalReports = totalReports,
            CurrentStreak = currentStreak,
            LongestStreak = Math.Max(longestStreak, profile?.LongestStreak ?? 0),
            TotalCoins = totalCoins,
            CoinsEarnedToday = todayCoins,
            CompletionRatePercent = Math.Round(completionRate, 1),
            FirstReportDate = firstReport,
            LastReportDate = lastReport
        };
    }

    private static int CalculateCurrentStreak(DateOnly today, HashSet<DateOnly> reportDates)
    {
        var streak = 0;
        var checkDate = today;

        // If today is a weekend, start from last Friday
        while (checkDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            checkDate = checkDate.AddDays(-1);

        // If today is a weekday and no report yet, start from yesterday
        if (!reportDates.Contains(checkDate))
            checkDate = PreviousWeekday(checkDate);

        while (reportDates.Contains(checkDate))
        {
            streak++;
            checkDate = PreviousWeekday(checkDate);
        }

        return streak;
    }

    private static int CalculateLongestStreak(List<DateOnly> sortedDates)
    {
        if (sortedDates.Count == 0) return 0;

        var longest = 1;
        var current = 1;

        for (int i = 1; i < sortedDates.Count; i++)
        {
            var expected = NextWeekday(sortedDates[i - 1]);
            if (sortedDates[i] == expected)
            {
                current++;
                if (current > longest)
                    longest = current;
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }

    private static int CalculateCoins(List<DateOnly> sortedDates)
    {
        if (sortedDates.Count == 0) return 0;

        var totalCoins = 0;
        var streak = 0;

        for (int i = 0; i < sortedDates.Count; i++)
        {
            if (i > 0 && sortedDates[i] == NextWeekday(sortedDates[i - 1]))
                streak++;
            else
                streak = 1;

            totalCoins += 1 + CalculateStreakBonus(streak);
        }

        return totalCoins;
    }

    private static int CalculateStreakBonus(int streak) => streak switch
    {
        >= 20 => 5,
        >= 10 => 3,
        >= 5 => 2,
        >= 3 => 1,
        _ => 0
    };

    private static DateOnly PreviousWeekday(DateOnly date)
    {
        var prev = date.AddDays(-1);
        while (prev.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            prev = prev.AddDays(-1);
        return prev;
    }

    private static DateOnly NextWeekday(DateOnly date)
    {
        var next = date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);
        return next;
    }

    private static int CountWeekdays(DateOnly from, DateOnly to)
    {
        var count = 0;
        var d = from;
        while (d <= to)
        {
            if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
            d = d.AddDays(1);
        }
        return count;
    }
}
