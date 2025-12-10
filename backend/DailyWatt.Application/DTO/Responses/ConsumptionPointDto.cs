namespace DailyWatt.Application.DTO.Responses;

/// <summary>
/// Generic consumption point DTO for application layer.
/// </summary>
public class ConsumptionPointDto
{
  public DateTime TimestampUtc { get; set; }
  public double Kwh { get; set; }
}