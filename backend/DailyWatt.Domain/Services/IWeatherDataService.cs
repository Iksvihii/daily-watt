using DailyWatt.Domain.Entities;

namespace DailyWatt.Domain.Services;

/// <summary>
/// Service for persisting and retrieving cached daily weather data.
/// </summary>
public interface IWeatherDataService
{
  Task<IReadOnlyList<WeatherDay>> GetAsync(Guid userId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);

  Task UpsertAsync(Guid userId, IEnumerable<WeatherDay> weatherDays, CancellationToken ct = default);

  Task DeleteAllAsync(Guid userId, CancellationToken ct = default);
}