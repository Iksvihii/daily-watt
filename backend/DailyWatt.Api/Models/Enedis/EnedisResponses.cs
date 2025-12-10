namespace DailyWatt.Api.Models.Enedis;

/// <summary>
/// Response containing import job information.
/// </summary>
public class ImportJobResponse
{
  /// <summary>
  /// Unique identifier of the import job.
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Status of the import job (Pending, Running, Completed, Failed).
  /// </summary>
  public string Status { get; set; } = string.Empty;

  /// <summary>
  /// Number of data points successfully imported.
  /// </summary>
  public int ImportedCount { get; set; }

  /// <summary>
  /// Timestamp when the job was created.
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Timestamp when the job completed (null if still running).
  /// </summary>
  public DateTime? CompletedAt { get; set; }

  /// <summary>
  /// Error code if the job failed.
  /// </summary>
  public string? ErrorCode { get; set; }

  /// <summary>
  /// Error message if the job failed.
  /// </summary>
  public string? ErrorMessage { get; set; }
}

/// <summary>
/// Indicates if Enedis credentials are configured for the user.
/// </summary>
public class EnedisStatusResponse
{
  /// <summary>
  /// True when credentials and meter number are stored.
  /// </summary>
  public bool Configured { get; set; }

  /// <summary>
  /// Optional meter number for display.
  /// </summary>
  public string? MeterNumber { get; set; }

  /// <summary>
  /// Last update timestamp in UTC.
  /// </summary>
  public DateTime? UpdatedAt { get; set; }
}
