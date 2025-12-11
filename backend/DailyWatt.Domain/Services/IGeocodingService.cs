namespace DailyWatt.Domain.Services;

/// <summary>
/// Service for finding cities and converting them to geographic coordinates.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Geocodes a city name to latitude and longitude coordinates of the city center.
    /// </summary>
    /// <param name="city">The city name (French cities only)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (latitude, longitude) or null if not found</returns>
    Task<(double latitude, double longitude)?> GeocodeAsync(
        string city,
        CancellationToken ct = default);

    /// <summary>
    /// Gets autocomplete suggestions for French city names.
    /// </summary>
    /// <param name="query">Partial city name query</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of French city name suggestions</returns>
    Task<List<string>> GetCitySuggestionsAsync(
        string query,
        CancellationToken ct = default);
}
