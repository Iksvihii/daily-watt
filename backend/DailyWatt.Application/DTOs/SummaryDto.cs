namespace DailyWatt.Application.DTOs;

/// <summary>
/// Generic summary DTO for application layer.
/// </summary>
public class SummaryDto
{
  public double TotalKwh { get; set; }
  public double AvgKwhPerDay { get; set; }
  public double MaxDayKwh { get; set; }
  public DateOnly? MaxDay { get; set; }
}
