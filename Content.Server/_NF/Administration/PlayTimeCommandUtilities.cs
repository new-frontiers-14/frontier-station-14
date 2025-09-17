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
    /// <exception cref="OverflowException">Thrown when the time value would overflow</exception>
    /// <exception cref="ArgumentException">Thrown when the time string format is invalid</exception>
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
        {
            // Check for overflow when converting days to minutes
            var dayMinutes = days * 24 * 60;
            if (double.IsInfinity(dayMinutes) || double.IsNaN(dayMinutes))
                throw new OverflowException($"Day value {days} is too large");
            
            totalMinutes = SafeAdd(totalMinutes, dayMinutes);
        }

        // Parse hours
        if (hourMatch.Success && double.TryParse(hourMatch.Groups[1].Value, out var hours))
        {
            var hourMinutes = hours * 60;
            if (double.IsInfinity(hourMinutes) || double.IsNaN(hourMinutes))
                throw new OverflowException($"Hour value {hours} is too large");
            
            totalMinutes = SafeAdd(totalMinutes, hourMinutes);
        }

        // Parse minutes
        if (minuteMatch.Success && double.TryParse(minuteMatch.Groups[1].Value, out var minutes))
        {
            if (double.IsInfinity(minutes) || double.IsNaN(minutes))
                throw new OverflowException($"Minute value {minutes} is invalid");
            
            totalMinutes = SafeAdd(totalMinutes, minutes);
        }

        // If no specific unit is provided, assume it's minutes
        if (!dayMatch.Success && !hourMatch.Success && !minuteMatch.Success &&
            double.TryParse(timeString, out var plainMinutes))
        {
            if (double.IsInfinity(plainMinutes) || double.IsNaN(plainMinutes))
                throw new OverflowException($"Minute value {plainMinutes} is invalid");
            
            totalMinutes = plainMinutes;
        }

        // Final validation
        if (double.IsInfinity(totalMinutes) || double.IsNaN(totalMinutes))
            throw new OverflowException("Total time calculation resulted in invalid value");

        return totalMinutes;
    }

    /// <summary>
    /// Safely adds two double values, checking for overflow
    /// </summary>
    private static double SafeAdd(double a, double b)
    {
        var result = a + b;
        
        if (double.IsInfinity(result) || double.IsNaN(result))
            throw new OverflowException($"Addition of {a} + {b} resulted in overflow");
        
        return result;
    }
}
