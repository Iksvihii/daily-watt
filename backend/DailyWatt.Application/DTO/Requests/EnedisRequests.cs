using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Application.DTO.Requests;

/// <summary>
/// Request to save Enedis user credentials.
/// Includes geographic coordinates for the Linky meter location.
/// </summary>
public record SaveEnedisCredentialsRequest
{
  [Required(ErrorMessage = "Login is required")]
  [StringLength(255)]
  public required string Login { get; init; }

  [Required(ErrorMessage = "Password is required")]
  [StringLength(255)]
  public required string Password { get; init; }

  [Required(ErrorMessage = "Meter number is required")]
  [StringLength(64)]
  public required string MeterNumber { get; init; }

  [StringLength(500)]
  public string? Address { get; init; }

  [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
  public double? Latitude { get; init; }

  [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
  public double? Longitude { get; init; }
}

/// <summary>
/// Request to create an import job for consumption data from Enedis.
/// </summary>
public record CreateImportJobRequest
{
  [Required(ErrorMessage = "FromUtc is required")]
  public required DateTime FromUtc { get; init; }

  [Required(ErrorMessage = "ToUtc is required")]
  public required DateTime ToUtc { get; init; }
}