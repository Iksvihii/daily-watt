namespace DailyWatt.Domain.Entities;

public class Measurement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public double Kwh { get; set; }
    public string Source { get; set; } = "enedis";

    public DailyWattUser? User { get; set; }
}
