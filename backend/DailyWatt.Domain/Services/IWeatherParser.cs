using System.Text.Json;
using DailyWatt.Domain.Entities;

namespace DailyWatt.Domain.Services;

/// <summary>
/// Interface for parsing weather data from external sources.
/// Separates parsing logic from persistence and retrieval logic.
/// </summary>
public interface IWeatherParser
{
  /// <summary>
  /// Parses weather data from a JSON document.
  /// </summary>
  List<WeatherDay> ParseFromJson(JsonDocument doc, string source);

  /// <summary>
  /// Generates fallback weather data when API fetch fails.
  /// </summary>
  List<WeatherDay> GenerateFallback(DateOnly from, DateOnly to, string source);
}
