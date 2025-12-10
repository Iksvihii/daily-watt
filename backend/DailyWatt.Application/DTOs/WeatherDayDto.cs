namespace DailyWatt.Application.DTOs;

/// <summary>
/// Generic weather day DTO for application layer.
/// </summary>
public class WeatherDayDto
{
  public string Date { get; set; } = string.Empty;
  public double TempAvg { get; set; }
  public double TempMin { get; set; }
  public double TempMax { get; set; }
  public string Source { get; set; } = string.Empty;
}
