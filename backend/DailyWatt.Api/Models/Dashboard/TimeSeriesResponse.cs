namespace DailyWatt.Api.Models.Dashboard;

public class TimeSeriesResponse
{
    public List<ConsumptionPointDto> Consumption { get; set; } = new();
    public List<WeatherDayDto>? Weather { get; set; }
    public SummaryDto Summary { get; set; } = new();
}
