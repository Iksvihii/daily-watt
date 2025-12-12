using System.Text.Json.Serialization;
using DailyWatt.Domain.Services;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Weather provider service using Open-Meteo Archive API.
/// Fetches historical weather data without limitations or authentication.
/// </summary>
public class OpenMeteoWeatherService : IWeatherProviderService
{
  private readonly HttpClient _httpClient;
  // Using the Archive API - free, unlimited, no authentication needed
  private const string ArchiveApiUrl = "https://archive-api.open-meteo.com/v1/archive";

  public OpenMeteoWeatherService(HttpClient httpClient)
  {
    _httpClient = httpClient;
  }

  public async Task<List<WeatherData>> GetWeatherAsync(
      double latitude,
      double longitude,
      DateOnly fromDate,
      DateOnly toDate,
      CancellationToken ct = default)
  {
    try
    {
      // Format coordinates using invariant culture to ensure decimal points (not commas)
      var latStr = latitude.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
      var lngStr = longitude.ToString("G", System.Globalization.CultureInfo.InvariantCulture);

      // Use the Archive API with date range - free, unlimited historical data
      // Works for any date range in the past
      var url = $"{ArchiveApiUrl}?" +
          $"latitude={latStr}&" +
          $"longitude={lngStr}&" +
          $"start_date={fromDate:yyyy-MM-dd}&" +
          $"end_date={toDate:yyyy-MM-dd}&" +
          $"daily=temperature_2m_max,temperature_2m_min,temperature_2m_mean&" +
          $"temperature_unit=celsius&" +
          $"timezone=auto";

      var response = await _httpClient.GetAsync(url, ct);

      if (!response.IsSuccessStatusCode)
        return new List<WeatherData>();

      var content = await response.Content.ReadAsStringAsync(ct);
      var result = System.Text.Json.JsonSerializer.Deserialize<OpenMeteoResponse>(content);

      if (result?.Daily == null || result.Daily.Times.Count == 0)
        return new List<WeatherData>();

      var weatherData = new List<WeatherData>();
      for (int i = 0; i < result.Daily.Times.Count; i++)
      {
        // Skip entries with missing temperature data
        if (!result.Daily.TempMean[i].HasValue || !result.Daily.TempMin[i].HasValue || !result.Daily.TempMax[i].HasValue)
          continue;

        var date = DateOnly.ParseExact(result.Daily.Times[i], "yyyy-MM-dd");
        weatherData.Add(new WeatherData(
            date.ToString("yyyy-MM-dd"),
            result.Daily.TempMean[i]!.Value,
            result.Daily.TempMin[i]!.Value,
            result.Daily.TempMax[i]!.Value,
            "open-meteo"
        ));
      }

      return weatherData;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Weather fetch error: {ex.Message}");
      return new List<WeatherData>();
    }
  }

  private class OpenMeteoResponse
  {
    [JsonPropertyName("daily")]
    public DailyData? Daily { get; set; }
  }

  private class DailyData
  {
    [JsonPropertyName("time")]
    public List<string> Times { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<double?> TempMax { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<double?> TempMin { get; set; } = new();

    [JsonPropertyName("temperature_2m_mean")]
    public List<double?> TempMean { get; set; } = new();
  }
}
