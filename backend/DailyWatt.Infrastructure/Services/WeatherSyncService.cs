using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Synchronizes cached weather data for missing dates only.
/// </summary>
public class WeatherSyncService : IWeatherSyncService
{
  private readonly IWeatherDataService _weatherDataService;
  private readonly IWeatherProviderService _weatherProviderService;

  public WeatherSyncService(IWeatherDataService weatherDataService, IWeatherProviderService weatherProviderService)
  {
    _weatherDataService = weatherDataService;
    _weatherProviderService = weatherProviderService;
  }

  public async Task EnsureWeatherAsync(
      Guid userId,
      double latitude,
      double longitude,
      DateOnly fromDate,
      DateOnly toDate,
      CancellationToken ct = default)
  {
    if (fromDate > toDate)
    {
      return;
    }

    var cached = await _weatherDataService.GetAsync(userId, fromDate, toDate, ct);
    var missingIntervals = GetMissingIntervals(fromDate, toDate, cached.Select(x => x.Date));

    if (missingIntervals.Count == 0)
    {
      return;
    }

    var toPersist = new List<WeatherDay>();

    foreach (var (missingFrom, missingTo) in missingIntervals)
    {
      var fetched = await _weatherProviderService.GetWeatherAsync(latitude, longitude, missingFrom, missingTo, ct);

      foreach (var weather in fetched)
      {
        toPersist.Add(new WeatherDay
        {
          UserId = userId,
          Date = DateOnly.ParseExact(weather.Date, "yyyy-MM-dd"),
          TempAvg = weather.TempAvg,
          TempMin = weather.TempMin,
          TempMax = weather.TempMax,
          Source = weather.Source,
          Latitude = latitude,
          Longitude = longitude,
          CreatedAtUtc = DateTime.UtcNow
        });
      }
    }

    if (toPersist.Count > 0)
    {
      await _weatherDataService.UpsertAsync(userId, toPersist, ct);
    }
  }

  private static List<(DateOnly From, DateOnly To)> GetMissingIntervals(DateOnly fromDate, DateOnly toDate, IEnumerable<DateOnly> existingDates)
  {
    var existing = existingDates.ToHashSet();
    var intervals = new List<(DateOnly From, DateOnly To)>();

    DateOnly? start = null;
    for (var date = fromDate; date <= toDate; date = date.AddDays(1))
    {
      if (!existing.Contains(date))
      {
        start ??= date;
      }
      else if (start.HasValue)
      {
        intervals.Add((start.Value, date.AddDays(-1)));
        start = null;
      }
    }

    if (start.HasValue)
    {
      intervals.Add((start.Value, toDate));
    }

    return intervals;
  }
}