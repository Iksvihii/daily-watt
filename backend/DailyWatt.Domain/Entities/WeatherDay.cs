namespace DailyWatt.Domain.Entities;

/// <summary>
/// Cached daily weather observation tied to a user's location.
/// </summary>
public class WeatherDay
{
  public Guid UserId { get; set; }
  public Guid MeterId { get; set; }
  public DateOnly Date { get; set; }
  public double TempAvg { get; set; }
  public double TempMin { get; set; }
  public double TempMax { get; set; }
  public string Source { get; set; } = string.Empty;
  public double Latitude { get; set; }
  public double Longitude { get; set; }
  public DateTime CreatedAtUtc { get; set; }

  public DailyWattUser? User { get; set; }
  public EnedisMeter? Meter { get; set; }
}