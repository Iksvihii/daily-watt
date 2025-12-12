using DailyWatt.Domain.Enums;

namespace DailyWatt.Api.Helpers;

/// <summary>
/// Helper to parse string granularity values to enum.
/// Supports: day, month, year.
/// Note: Requests for 30min/hour are coerced to day.
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
            "30min" or "30m" or "hour" or "1h" => Granularity.Day,
            "day" or "daily" => Granularity.Day,
            "month" or "monthly" => Granularity.Month,
            "year" or "annual" => Granularity.Year,
            _ => Granularity.Day
        };
    }
}
