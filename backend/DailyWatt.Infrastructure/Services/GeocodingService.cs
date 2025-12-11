using System.Globalization;
using System.Text.Json.Serialization;
using DailyWatt.Domain.Services;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Geocoding service using OpenStreetMap Nominatim API.
/// Searches for French cities and converts them to geographic coordinates.
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
      string city,
      CancellationToken ct = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(city))
        return null;

      // Search for French cities only
      var url = $"{NominatimApiUrl}/search?q={Uri.EscapeDataString(city)}&format=json&limit=1&countrycodes=fr&extratags=1";
      var response = await _httpClient.GetAsync(url, ct);

      if (!response.IsSuccessStatusCode)
        return null;

      var content = await response.Content.ReadAsStringAsync(ct);
      var results = System.Text.Json.JsonSerializer.Deserialize<List<NominatimResult>>(content);

      if (results?.Count > 0)
      {
        var result = results[0];
        return (double.Parse(result.Lat, CultureInfo.InvariantCulture), double.Parse(result.Lon, CultureInfo.InvariantCulture));
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

  public async Task<List<string>> GetCitySuggestionsAsync(
      string query,
      CancellationToken ct = default)
  {
    try
    {
      if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        return new List<string>();

      // Search for French cities with viewbox to focus on France
      // viewbox format: left,top,right,bottom (lon,lat,lon,lat)
      // France approximate bounds: left=-8, top=51, right=8, bottom=41
      // Remove bounded=1 to allow variations like "Fontenille" and "Fontenilles"
      var url = $"{NominatimApiUrl}/search?q={Uri.EscapeDataString(query)}&format=json&limit=50&countrycodes=fr&viewbox=-8,51,8,41&extratags=1&namedetails=1";

      var response = await _httpClient.GetAsync(url, ct);

      if (!response.IsSuccessStatusCode)
        return new List<string>();

      var content = await response.Content.ReadAsStringAsync(ct);
      var results = System.Text.Json.JsonSerializer.Deserialize<List<NominatimResult>>(content);

      // Filter to only cities, towns, and villages by addresstype
      // Nominatim returns cities as boundary/administrative with addresstype=city/town/village
      var suggestions = results?
          .Where(r => r.AddressType == "city" || r.AddressType == "town" || r.AddressType == "village")
          .Select(r => FormatCitySuggestion(r))
          .Distinct()
          .Take(10)
          .ToList() ?? new List<string>();

      return suggestions;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"City suggestions error: {ex.Message}");
      return new List<string>();
    }
  }

  public async Task<string?> ReverseGeocodeAsync(
      double latitude,
      double longitude,
      CancellationToken ct = default)
  {
    try
    {
      // Reverse geocoding: convert coordinates to city name
      var url = $"{NominatimApiUrl}/reverse?format=json&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&zoom=10&addressdetails=1";

      var response = await _httpClient.GetAsync(url, ct);

      if (!response.IsSuccessStatusCode)
        return null;

      var content = await response.Content.ReadAsStringAsync(ct);
      var result = System.Text.Json.JsonSerializer.Deserialize<NominatimReverseResult>(content);

      if (result == null)
        return null;

      // Try to extract city/town/village name from address
      if (result.Address != null)
      {
        // Priority: city > town > village > hamlet
        if (!string.IsNullOrWhiteSpace(result.Address.City))
          return FormatCityWithPostalCode(result.Address.City, result.Address.Postcode);

        if (!string.IsNullOrWhiteSpace(result.Address.Town))
          return FormatCityWithPostalCode(result.Address.Town, result.Address.Postcode);

        if (!string.IsNullOrWhiteSpace(result.Address.Village))
          return FormatCityWithPostalCode(result.Address.Village, result.Address.Postcode);

        if (!string.IsNullOrWhiteSpace(result.Address.Hamlet))
          return FormatCityWithPostalCode(result.Address.Hamlet, result.Address.Postcode);
      }

      return null;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Reverse geocoding error: {ex.Message}");
      return null;
    }
  }

  private string FormatCityWithPostalCode(string city, string? postcode)
  {
    if (!string.IsNullOrWhiteSpace(postcode))
      return $"{city} ({postcode})";

    return city;
  }

  private string FormatCitySuggestion(NominatimResult result)
  {
    var cityName = result.DisplayName.Split(",")[0].Trim();
    var postalCode = ExtractPostalCode(result.DisplayName);

    if (!string.IsNullOrWhiteSpace(postalCode))
      return $"{cityName} ({postalCode})";

    return cityName;
  }

  private string ExtractPostalCode(string displayName)
  {
    // displayName format: "CityName, ..., PostalCode, CountryName"
    // Try to extract postal code (typically 5 digits for France)
    var parts = displayName.Split(",");

    foreach (var part in parts)
    {
      var trimmed = part.Trim();
      // Match French postal codes (5 digits)
      if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{5}$"))
        return trimmed;
    }

    return string.Empty;
  }

  private class NominatimResult
  {
    [JsonPropertyName("lat")]
    public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Lon { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("addresstype")]
    public string AddressType { get; set; } = string.Empty;
  }

  private class NominatimReverseResult
  {
    [JsonPropertyName("address")]
    public NominatimAddress? Address { get; set; }
  }

  private class NominatimAddress
  {
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("town")]
    public string? Town { get; set; }

    [JsonPropertyName("village")]
    public string? Village { get; set; }

    [JsonPropertyName("hamlet")]
    public string? Hamlet { get; set; }

    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }
  }
}
