using System;
using System.Text.RegularExpressions;

namespace Content.Server._NF.Administration;

/// <summary>
/// Utility methods for handling play time commands and time string parsing
/// </summary>
public static class PlayTimeCommandUtilities
{
    // Pre-compiled regex patterns to avoid constant re-parsing
    private static readonly Regex DayRegex = new(@"(\d+\.?\d*)d", RegexOptions.Compiled);
    private static readonly Regex HourRegex = new(@"(\d+\.?\d*)h", RegexOptions.Compiled);
    private static readonly Regex MinuteRegex = new(@"(\d+\.?\d*)m", RegexOptions.Compiled);

    /// <summary>
    /// Parses a time string into minutes.
    /// </summary>
    /// <param name="timeString">Time string in a format like "1d 2h 30m" or "90m" or "1.5h"</param>
    /// <returns>The total number of minutes represented by the string</returns>
    public static double CountMinutes(string timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
            return 0;

        double totalMinutes = 0;

        // Use pre-compiled regex patterns
        var dayMatch = DayRegex.Match(timeString);
        var hourMatch = HourRegex.Match(timeString);
        var minuteMatch = MinuteRegex.Match(timeString);

        // Parse days
        if (dayMatch.Success && double.TryParse(dayMatch.Groups[1].Value, out var days))
            totalMinutes += days * 24 * 60;

        // Parse hours
        if (hourMatch.Success && double.TryParse(hourMatch.Groups[1].Value, out var hours))
            totalMinutes += hours * 60;

        // Parse minutes
        if (minuteMatch.Success && double.TryParse(minuteMatch.Groups[1].Value, out var minutes))
            totalMinutes += minutes;

        // If no specific unit is provided, assume it's minutes
        if (!dayMatch.Success && !hourMatch.Success && !minuteMatch.Success &&
            double.TryParse(timeString, out var plainMinutes))
            totalMinutes = plainMinutes;

        return totalMinutes;
    }
}
