using System.Text.Json;
using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Service for parsing weather data from various formats.
/// Handles JSON parsing and fallback data generation.
/// </summary>
public class WeatherParser : IWeatherParser
{
  public List<WeatherDay> ParseFromJson(JsonDocument doc, string source)
  {
    var root = doc.RootElement;
    if (!root.TryGetProperty("daily", out var daily))
    {
      return [];
    }

    var dates = daily.GetProperty("time").EnumerateArray().ToList();
    var maxes = daily.GetProperty("temperature_2m_max").EnumerateArray().ToList();
    var mins = daily.GetProperty("temperature_2m_min").EnumerateArray().ToList();
    var avgs = daily.TryGetProperty("temperature_2m_mean", out var mean) ? mean.EnumerateArray().ToList() : new List<JsonElement>();

    var list = new List<WeatherDay>();
    for (var i = 0; i < dates.Count; i++)
    {
      var date = DateOnly.Parse(dates[i].GetString()!);
      var max = maxes.ElementAtOrDefault(i).GetDouble();
      var min = mins.ElementAtOrDefault(i).GetDouble();
      var avg = avgs.Count > i ? avgs[i].GetDouble() : (max + min) / 2;

      list.Add(new WeatherDay
      {
        Date = date,
        TempMax = max,
        TempMin = min,
        TempAvg = avg,
        Source = source
      });
    }

    return list;
  }

  public List<WeatherDay> GenerateFallback(DateOnly from, DateOnly to, string source)
  {
    var result = new List<WeatherDay>();
    var random = new Random(42);
    for (var date = from; date <= to; date = date.AddDays(1))
    {
      var tempAvg = random.Next(-5, 30);
      result.Add(new WeatherDay
      {
        Date = date,
        TempAvg = tempAvg,
        TempMin = tempAvg - 3,
        TempMax = tempAvg + 3,
        Source = source
      });
    }

    return result;
  }
}
