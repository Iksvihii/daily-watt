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

/// <summary>
/// Basic user profile information.
/// </summary>
public class UserProfileResponse
{
  /// <summary>
  /// Account email (also used as login identifier).
  /// </summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>
  /// Public username displayed in the app.
  /// </summary>
  public string Username { get; set; } = string.Empty;
}
