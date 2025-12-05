namespace DailyWatt.Infrastructure.Settings;

public class WeatherOptions
{
    public const string SectionName = "Weather";

    public double Latitude { get; set; } = 48.8566;
    public double Longitude { get; set; } = 2.3522;
    public string BaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";
    public string Source { get; set; } = "open-meteo";
}
