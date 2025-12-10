using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Api.Models.Enedis;

/// <summary>
/// Request to save Enedis user credentials securely.
/// </summary>
public class SaveEnedisCredentialsRequest
{
  /// <summary>
  /// Enedis login username.
  /// </summary>
  [Required(ErrorMessage = "Login is required")]
  [StringLength(255)]
  public required string Login { get; init; }

  /// <summary>
  /// Enedis account password.
  /// </summary>
  [Required(ErrorMessage = "Password is required")]
  [StringLength(255)]
  public required string Password { get; init; }
}

/// <summary>
/// Request to create an import job for consumption data from Enedis.
/// </summary>
public class CreateImportJobRequest
{
  /// <summary>
  /// Start date/time (UTC) for the import range.
  /// </summary>
  [Required(ErrorMessage = "FromUtc is required")]
  public required DateTime FromUtc { get; init; }

  /// <summary>
  /// End date/time (UTC) for the import range.
  /// </summary>
  [Required(ErrorMessage = "ToUtc is required")]
  public required DateTime ToUtc { get; init; }
}
