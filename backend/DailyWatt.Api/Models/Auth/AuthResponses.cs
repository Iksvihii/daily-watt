namespace DailyWatt.Api.Models.Auth;

/// <summary>
/// Authentication response containing the JWT token.
/// </summary>
public class AuthResponse
{
  /// <summary>
  /// JWT bearer token for authenticated requests.
  /// </summary>
  public string Token { get; set; } = string.Empty;
}
