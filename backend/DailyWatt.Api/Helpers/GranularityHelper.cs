using DailyWatt.Domain.Enums;

namespace DailyWatt.Api.Helpers;

/// <summary>
/// Helper to parse string granularity values to enum.
/// Supports: 30min, hour, day, month, year.
/// </summary>
public static class GranularityHelper
{
    public static Granularity Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Granularity.Day;
        }

        return value.ToLowerInvariant() switch
        {
            "30min" or "30m" => Granularity.ThirtyMinutes,
            "hour" or "1h" => Granularity.Hour,
            "day" or "daily" => Granularity.Day,
            "month" or "monthly" => Granularity.Month,
            "year" or "annual" => Granularity.Year,
            _ => Granularity.Day
        };
    }
}
