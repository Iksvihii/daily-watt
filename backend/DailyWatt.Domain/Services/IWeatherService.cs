using DailyWatt.Domain.Entities;

namespace DailyWatt.Domain.Services;

public interface IWeatherService
{
    Task EnsureWeatherRangeAsync(Guid userId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
    Task<IReadOnlyList<WeatherDay>> GetRangeAsync(Guid userId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
}
