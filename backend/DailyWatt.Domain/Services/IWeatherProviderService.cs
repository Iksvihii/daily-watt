namespace DailyWatt.Domain.Services;

/// <summary>
/// Simple weather data model for domain layer.
/// </summary>
public record WeatherData(
    string Date,
    double TempAvg,
    double TempMin,
    double TempMax,
    string Source = "open-meteo");

/// <summary>
/// Service for fetching real-time weather data from external providers.
/// Data is retrieved on-demand and not stored in the database.
/// </summary>
public interface IWeatherProviderService
{
  /// <summary>
  /// Gets weather data for a geographic location and date range.
  /// </summary>
  /// <param name="latitude">Geographic latitude</param>
  /// <param name="longitude">Geographic longitude</param>
  /// <param name="fromDate">Start date</param>
  /// <param name="toDate">End date</param>
  /// <param name="ct">Cancellation token</param>
  /// <returns>List of daily weather data</returns>
  Task<List<WeatherData>> GetWeatherAsync(
      double latitude,
      double longitude,
      DateOnly fromDate,
      DateOnly toDate,
      CancellationToken ct = default);
}
