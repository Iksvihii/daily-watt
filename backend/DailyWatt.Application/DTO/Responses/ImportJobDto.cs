namespace DailyWatt.Application.DTO.Responses;

/// <summary>
/// Generic import job DTO for application layer.
/// </summary>
public class ImportJobDto
{
  public Guid Id { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? CompletedAt { get; set; }
  public string? ErrorCode { get; set; }
  public string? ErrorMessage { get; set; }
  public int ImportedCount { get; set; }
  public string Status { get; set; } = string.Empty;
}