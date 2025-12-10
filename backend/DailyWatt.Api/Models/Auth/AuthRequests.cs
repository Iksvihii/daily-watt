using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Api.Models.Auth;

/// <summary>
/// Login request with email and password.
/// </summary>
public class LoginRequest
{
  /// <summary>
  /// User email address.
  /// </summary>
  [Required(ErrorMessage = "Email is required")]
  [EmailAddress(ErrorMessage = "Invalid email format")]
  public required string Email { get; init; }

  /// <summary>
  /// User password.
  /// </summary>
  [Required(ErrorMessage = "Password is required")]
  [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
  public required string Password { get; init; }
}

/// <summary>
/// User registration request.
/// </summary>
public class RegisterRequest
{
  /// <summary>
  /// Email address for the new account.
  /// </summary>
  [Required(ErrorMessage = "Email is required")]
  [EmailAddress(ErrorMessage = "Invalid email format")]
  public required string Email { get; init; }

  /// <summary>
  /// Password for the new account.
  /// </summary>
  [Required(ErrorMessage = "Password is required")]
  [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
  public required string Password { get; init; }
}
