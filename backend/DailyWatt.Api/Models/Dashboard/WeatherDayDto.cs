namespace DailyWatt.Api.Models.Dashboard;

public class WeatherDayDto
{
    public DateOnly Date { get; set; }
    public double TempAvg { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public string Source { get; set; } = string.Empty;
}
