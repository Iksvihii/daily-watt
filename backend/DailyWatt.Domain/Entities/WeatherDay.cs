namespace DailyWatt.Domain.Entities;

public class WeatherDay
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public double TempAvg { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public string Source { get; set; } = "open-meteo";

    public DailyWattUser? User { get; set; }
}
