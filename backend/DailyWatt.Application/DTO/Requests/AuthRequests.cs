using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Application.DTO.Requests;

/// <summary>
/// User registration request payload.
/// </summary>
public record RegisterRequest
{
  [Required(ErrorMessage = "Email is required")]
  [EmailAddress(ErrorMessage = "Invalid email format")]
  public required string Email { get; init; }

  [Required(ErrorMessage = "Username is required")]
  [StringLength(64, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 64 characters")]
  public required string Username { get; init; }

  [Required(ErrorMessage = "Password is required")]
  [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
  public required string Password { get; init; }
}

/// <summary>
/// Login request payload.
/// </summary>
public record LoginRequest
{
  [Required(ErrorMessage = "Email is required")]
  [EmailAddress(ErrorMessage = "Invalid email format")]
  public required string Email { get; init; }

  [Required(ErrorMessage = "Password is required")]
  [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
  public required string Password { get; init; }
}

/// <summary>
/// Request to update profile information.
/// </summary>
public record UpdateProfileRequest
{
  [Required(ErrorMessage = "Username is required")]
  [StringLength(64, MinimumLength = 2, ErrorMessage = "Username must be between 2 and 64 characters")]
  public required string Username { get; init; }
}

/// <summary>
/// Request to change password.
/// </summary>
public record ChangePasswordRequest
{
  [Required(ErrorMessage = "Current password is required")]
  public required string CurrentPassword { get; init; }

  [Required(ErrorMessage = "New password is required")]
  [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
  public required string NewPassword { get; init; }
}