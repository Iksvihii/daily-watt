namespace DailyWatt.Api.Models.Dashboard;

public class ConsumptionPointDto
{
    public DateTime TimestampUtc { get; set; }
    public double Kwh { get; set; }
}
