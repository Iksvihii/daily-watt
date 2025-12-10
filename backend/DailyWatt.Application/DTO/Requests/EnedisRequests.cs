using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Application.DTO.Requests;

/// <summary>
/// Request to save Enedis user credentials.
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