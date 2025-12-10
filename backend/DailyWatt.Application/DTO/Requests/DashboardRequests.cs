using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Application.DTO.Requests;

/// <summary>
/// Request to retrieve time series data with optional weather.
/// </summary>
public record GetTimeSeriesRequest
{
  [Required(ErrorMessage = "From date is required")]
  public required DateTime From { get; init; }

  [Required(ErrorMessage = "To date is required")]
  public required DateTime To { get; init; }

  [Required(ErrorMessage = "Granularity is required")]
  [StringLength(10)]
  public required string Granularity { get; init; }

  public DateTime? StartDate { get; init; }

  public DateTime? EndDate { get; init; }

  public bool WithWeather { get; init; }
}
