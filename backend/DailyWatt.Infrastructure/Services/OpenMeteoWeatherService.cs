using System.Text.Json.Serialization;
using DailyWatt.Domain.Services;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Weather provider service using Open-Meteo API.
/// Fetches real-time weather data without storing it.
/// </summary>
public class OpenMeteoWeatherService : IWeatherProviderService
{
  private readonly HttpClient _httpClient;
  private const string ApiUrl = "https://archive-api.open-meteo.com/v1/archive";

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
      var url = $"{ApiUrl}?" +
          $"latitude={latitude}&" +
          $"longitude={longitude}&" +
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
        var date = DateOnly.ParseExact(result.Daily.Times[i], "yyyy-MM-dd");
        weatherData.Add(new WeatherData(
            date.ToString("yyyy-MM-dd"),
            result.Daily.TempMean[i],
            result.Daily.TempMin[i],
            result.Daily.TempMax[i],
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
    public List<double> TempMax { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<double> TempMin { get; set; } = new();

    [JsonPropertyName("temperature_2m_mean")]
    public List<double> TempMean { get; set; } = new();
  }
}
