using DailyWatt.Domain.Entities;

namespace DailyWatt.Api.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
  /// <summary>
  /// Creates a JWT token for the given user.
  /// </summary>
  string CreateToken(DailyWattUser user);
}
