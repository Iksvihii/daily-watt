namespace DailyWatt.Application.DTO.Responses;

/// <summary>
/// Indicates whether Enedis credentials are configured for the user.
/// </summary>
public class EnedisStatus
{
  public bool Configured { get; set; }
  public string? MeterNumber { get; set; }
  public DateTime? UpdatedAt { get; set; }
}