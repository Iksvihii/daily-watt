namespace DailyWatt.Domain.Services;

/// <summary>
/// Service for converting addresses to geographic coordinates (geocoding).
/// </summary>
public interface IGeocodingService
{
  /// <summary>
  /// Geocodes an address to latitude and longitude coordinates.
  /// </summary>
  /// <param name="address">The address to geocode</param>
  /// <param name="ct">Cancellation token</param>
  /// <returns>Tuple of (latitude, longitude) or null if not found</returns>
  Task<(double latitude, double longitude)?> GeocodeAsync(
      string address,
      CancellationToken ct = default);

  /// <summary>
  /// Gets autocomplete suggestions for an address.
  /// </summary>
  /// <param name="query">Partial address query</param>
  /// <param name="countryCode">Optional country code to filter results (e.g., 'FR' for France)</param>
  /// <param name="ct">Cancellation token</param>
  /// <returns>List of address suggestions</returns>
  Task<List<string>> GetAddressSuggestionsAsync(
      string query,
      string? countryCode = null,
      CancellationToken ct = default);
}
