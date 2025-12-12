namespace DailyWatt.Domain.Services;

/// <summary>
/// Ensures cached weather coverage for a user and location within a date range.
/// Only missing dates are fetched from the provider.
/// </summary>
public interface IWeatherSyncService
{
  Task EnsureWeatherAsync(
      Guid userId,
      Guid meterId,
      double latitude,
      double longitude,
      DateOnly fromDate,
      DateOnly toDate,
      CancellationToken ct = default);
}