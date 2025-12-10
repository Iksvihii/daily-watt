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
  /// Public username displayed in the application.
  /// </summary>
  [Required(ErrorMessage = "Username is required")]
  [StringLength(64, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 64 characters")]
  public required string Username { get; init; }

  /// <summary>
  /// Password for the new account.
  /// </summary>
  [Required(ErrorMessage = "Password is required")]
  [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
  public required string Password { get; init; }
}

/// <summary>
/// Request to update user profile.
/// </summary>
public class UpdateProfileRequest
{
  /// <summary>
  /// New username to display.
  /// </summary>
  [Required(ErrorMessage = "Username is required")]
  [StringLength(64, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 64 characters")]
  public required string Username { get; init; }
}

/// <summary>
/// Request to change the user password.
/// </summary>
public class ChangePasswordRequest
{
  /// <summary>
  /// Current password used for verification.
  /// </summary>
  [Required(ErrorMessage = "Current password is required")]
  public required string CurrentPassword { get; init; }

  /// <summary>
  /// New password.
  /// </summary>
  [Required(ErrorMessage = "New password is required")]
  [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
  public required string NewPassword { get; init; }
}
