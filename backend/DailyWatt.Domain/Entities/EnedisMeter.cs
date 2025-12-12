namespace DailyWatt.Domain.Entities;

/// <summary>
/// Represents a user's Enedis meter (PRM) with optional metadata.
/// </summary>
public class EnedisMeter
{
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public string Prm { get; set; } = string.Empty;
  public string? Label { get; set; }
  public string? City { get; set; }
  public double? Latitude { get; set; }
  public double? Longitude { get; set; }
  public bool IsFavorite { get; set; }
  public DateTime CreatedAtUtc { get; set; }
  public DateTime UpdatedAtUtc { get; set; }

  public DailyWattUser? User { get; set; }
}