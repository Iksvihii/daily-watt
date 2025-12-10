namespace DailyWatt.Application.DTO.Responses;

/// <summary>
/// Response DTO for time series data.
/// </summary>
public class TimeSeriesResponse
{
  public List<ConsumptionPointDto> Consumption { get; set; } = new();
  public List<WeatherDayDto>? Weather { get; set; }
  public SummaryDto Summary { get; set; } = new();
}