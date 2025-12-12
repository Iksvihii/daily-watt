using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Application.DTO.Requests;

/// <summary>
/// Request to save Enedis user credentials (login and password only).
/// Meter information is managed separately via meter CRUD endpoints.
/// </summary>
public record SaveEnedisCredentialsRequest
{
  [Required(ErrorMessage = "Login is required")]
  [StringLength(255)]
  public required string Login { get; init; }

  [Required(ErrorMessage = "Password is required")]
  [StringLength(255)]
  public required string Password { get; init; }
}

/// <summary>
/// Request to create an import job for consumption data from Enedis.
/// </summary>
public record CreateImportJobRequest
{
  [Required(ErrorMessage = "MeterId is required")]
  public required Guid MeterId { get; init; }

  [Required(ErrorMessage = "FromUtc is required")]
  public required DateTime FromUtc { get; init; }

  [Required(ErrorMessage = "ToUtc is required")]
  public required DateTime ToUtc { get; init; }
}