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

/// <summary>
/// Coin rules:
/// - Same-day regular report: 1 coin + streak bonus
/// - Backdated regular report: 0 coins (useful for records, no reward)
/// - Leave entries (Holiday/Sick/Vacation): 0 coins, streak preserved (streak freeze)
/// - Future dates: blocked at UI level, cannot be entered
/// </summary>
public static class StatsEngine
{
    private record ReportInfo(DateOnly Date, bool IsLeave, bool WasOnTime);

    public static StatsResult Calculate()
    {
        var allDates = ReportStorage.GetAllReportDates()
            .Select(d => DateOnly.FromDateTime(d))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (allDates.Count == 0)
            return new StatsResult();

        // Build report info with leave/on-time status
        var reports = allDates.Select(d =>
        {
            var content = ReportStorage.LoadReport(d.ToDateTime(TimeOnly.MinValue)) ?? "";
            var isLeave = ReportStorage.IsLeaveEntry(content);
            var wasOnTime = ReportStorage.WasEnteredOnTime(d.ToDateTime(TimeOnly.MinValue));
            return new ReportInfo(d, isLeave, wasOnTime);
        }).ToList();

        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, App.AppTimeZone));

        var reportSet = allDates.ToHashSet();
        var leaveSet = reports.Where(r => r.IsLeave).Select(r => r.Date).ToHashSet();

        // Current streak (leave days don't break it, but don't count toward it)
        var currentStreak = CalculateCurrentStreak(today, reportSet, leaveSet);

        // Longest streak ever
        var longestStreak = CalculateLongestStreak(allDates, leaveSet);

        // Coins: only same-day non-leave reports earn coins
        var totalCoins = CalculateCoins(reports);

        // Today's coins
        var todayReport = reports.FirstOrDefault(r => r.Date == today);
        var todayCoins = 0;
        if (todayReport != null && !todayReport.IsLeave && todayReport.WasOnTime)
            todayCoins = 1 + CalculateStreakBonus(currentStreak);

        // Completion rate
        var firstReport = allDates[0];
        var totalWeekdays = CountWeekdays(firstReport, today);
        var completionRate = totalWeekdays > 0 ? (double)allDates.Count / totalWeekdays * 100 : 0;

        // Update profile
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
            TotalReports = allDates.Count,
            CurrentStreak = currentStreak,
            LongestStreak = Math.Max(longestStreak, profile?.LongestStreak ?? 0),
            TotalCoins = totalCoins,
            CoinsEarnedToday = todayCoins,
            CompletionRatePercent = Math.Round(completionRate, 1),
            FirstReportDate = firstReport,
            LastReportDate = allDates[^1]
        };
    }

    /// <summary>
    /// Current streak: counts consecutive weekdays with a report (or leave).
    /// Leave days don't break the streak but don't add to the count.
    /// Only actual reports increment the streak number.
    /// </summary>
    private static int CalculateCurrentStreak(DateOnly today, HashSet<DateOnly> reportDates, HashSet<DateOnly> leaveDates)
    {
        var streak = 0;
        var checkDate = today;

        // If today is a weekend, start from last Friday
        while (checkDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            checkDate = checkDate.AddDays(-1);

        // If today is a weekday with no report and no leave, start from previous weekday
        if (!reportDates.Contains(checkDate) && !leaveDates.Contains(checkDate))
            checkDate = PreviousWeekday(checkDate);

        while (true)
        {
            if (leaveDates.Contains(checkDate))
            {
                // Leave day: streak freeze — skip without breaking
                checkDate = PreviousWeekday(checkDate);
                continue;
            }

            if (reportDates.Contains(checkDate))
            {
                streak++;
                checkDate = PreviousWeekday(checkDate);
            }
            else
            {
                break; // No report, no leave — streak broken
            }
        }

        return streak;
    }

    /// <summary>
    /// Longest streak: same logic as current streak but scanned across all dates.
    /// </summary>
    private static int CalculateLongestStreak(List<DateOnly> sortedDates, HashSet<DateOnly> leaveDates)
    {
        if (sortedDates.Count == 0) return 0;

        var longest = 0;
        var current = 0;

        for (int i = 0; i < sortedDates.Count; i++)
        {
            if (leaveDates.Contains(sortedDates[i]))
            {
                // Leave: check if chain is still connected
                if (i > 0 && IsNextWeekdayOrLeaveChain(sortedDates[i - 1], sortedDates[i], leaveDates))
                {
                    // Don't increment streak count, but don't break it
                }
                else if (i == 0)
                {
                    // First entry is leave — no streak started
                }
                continue;
            }

            // Regular report
            if (i == 0 || !IsConnected(sortedDates, i, leaveDates))
            {
                current = 1;
            }
            else
            {
                current++;
            }

            if (current > longest)
                longest = current;
        }

        return longest;
    }

    /// <summary>
    /// Checks if sortedDates[i] is connected to the previous report via consecutive weekdays
    /// (with leave days bridging gaps).
    /// </summary>
    private static bool IsConnected(List<DateOnly> sortedDates, int i, HashSet<DateOnly> leaveDates)
    {
        // Walk backwards from sortedDates[i] to find the previous non-leave entry
        var current = PreviousWeekday(sortedDates[i]);
        while (leaveDates.Contains(current))
            current = PreviousWeekday(current);

        // Find the previous non-leave date in sortedDates
        for (int j = i - 1; j >= 0; j--)
        {
            if (!leaveDates.Contains(sortedDates[j]))
                return sortedDates[j] == current;
        }

        return false;
    }

    private static bool IsNextWeekdayOrLeaveChain(DateOnly prev, DateOnly current, HashSet<DateOnly> leaveDates)
    {
        var expected = NextWeekday(prev);
        while (expected < current)
        {
            if (!leaveDates.Contains(expected))
                return false;
            expected = NextWeekday(expected);
        }
        return expected == current;
    }

    /// <summary>
    /// Coins: only same-day (non-backdated) regular reports earn coins.
    /// Leave entries earn 0. Backdated entries earn 0.
    /// </summary>
    private static int CalculateCoins(List<ReportInfo> reports)
    {
        var totalCoins = 0;
        var streak = 0;

        // We need to track streak including leave freezes for bonus calculation
        var leaveDates = reports.Where(r => r.IsLeave).Select(r => r.Date).ToHashSet();

        for (int i = 0; i < reports.Count; i++)
        {
            var r = reports[i];

            if (r.IsLeave)
                continue; // 0 coins, but doesn't break streak

            // Calculate current streak position
            if (i > 0)
            {
                var prevNonLeave = reports.Take(i).LastOrDefault(p => !p.IsLeave);
                if (prevNonLeave != null && IsNextWeekdayOrLeaveChain(prevNonLeave.Date, r.Date, leaveDates))
                    streak++;
                else
                    streak = 1;
            }
            else
            {
                streak = 1;
            }

            // Only award coins if entered on time (not backdated)
            if (r.WasOnTime)
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
