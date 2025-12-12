using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Application.DTO.Requests;

/// <summary>
/// Request to create an Enedis meter.
/// </summary>
public record CreateMeterRequest
{
  [Required(ErrorMessage = "PRM is required")]
  [StringLength(64)]
  public required string Prm { get; init; }

  [StringLength(255)]
  public string? Label { get; init; }

  [StringLength(255)]
  public string? City { get; init; }

  [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
  public double? Latitude { get; init; }

  [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
  public double? Longitude { get; init; }

  public bool IsFavorite { get; init; }
}

/// <summary>
/// Request to update an Enedis meter.
/// </summary>
public record UpdateMeterRequest
{
  [Required(ErrorMessage = "PRM is required")]
  [StringLength(64)]
  public required string Prm { get; init; }

  [StringLength(255)]
  public string? Label { get; init; }

  [StringLength(255)]
  public string? City { get; init; }

  [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
  public double? Latitude { get; init; }

  [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
  public double? Longitude { get; init; }
}
