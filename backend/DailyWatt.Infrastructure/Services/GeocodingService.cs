using System.Text.Json.Serialization;
using DailyWatt.Domain.Services;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Geocoding service using OpenStreetMap Nominatim API.
/// Converts addresses to geographic coordinates.
/// </summary>
public class GeocodingService : IGeocodingService
{
  private readonly HttpClient _httpClient;
  private const string NominatimApiUrl = "https://nominatim.openstreetmap.org";
  private const string UserAgent = "DailyWatt/1.0";

  public GeocodingService(HttpClient httpClient)
  {
    _httpClient = httpClient;
    _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
  }

  public async Task<(double latitude, double longitude)?> GeocodeAsync(
      string address,
      CancellationToken ct = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(address))
        return null;

      var url = $"{NominatimApiUrl}/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
      var response = await _httpClient.GetAsync(url, ct);

      if (!response.IsSuccessStatusCode)
        return null;

      var content = await response.Content.ReadAsStringAsync(ct);
      var results = System.Text.Json.JsonSerializer.Deserialize<List<NominatimResult>>(content);

      if (results?.Count > 0)
      {
        var result = results[0];
        return (double.Parse(result.Lat), double.Parse(result.Lon));
      }

      return null;
    }
    catch (Exception ex)
    {
      // Log error but don't throw - geocoding failure shouldn't break the flow
      System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
      return null;
    }
  }

  public async Task<List<string>> GetAddressSuggestionsAsync(
      string query,
      CancellationToken ct = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        return new List<string>();

      var url = $"{NominatimApiUrl}/search?q={Uri.EscapeDataString(query)}&format=json&limit=5&dedupe=1";
      var response = await _httpClient.GetAsync(url, ct);

      if (!response.IsSuccessStatusCode)
        return new List<string>();

      var content = await response.Content.ReadAsStringAsync(ct);
      var results = System.Text.Json.JsonSerializer.Deserialize<List<NominatimResult>>(content);

      return results?
          .Select(r => r.DisplayName)
          .Take(5)
          .ToList() ?? new List<string>();
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Address suggestions error: {ex.Message}");
      return new List<string>();
    }
  }

  private class NominatimResult
  {
    [JsonPropertyName("lat")]
    public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Lon { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
  }
}
