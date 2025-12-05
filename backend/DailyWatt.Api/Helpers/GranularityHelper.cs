using DailyWatt.Domain.Enums;

namespace DailyWatt.Api.Helpers;

public static class GranularityHelper
{
    public static Granularity Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Granularity.ThirtyMinutes;
        }

        return value.ToLowerInvariant() switch
        {
            "30m" or "30min" or "30minutes" or "halfhour" or "thirtyminutes" => Granularity.ThirtyMinutes,
            "hour" or "1h" => Granularity.Hour,
            "day" or "daily" => Granularity.Day,
            _ => Granularity.ThirtyMinutes
        };
    }
}
