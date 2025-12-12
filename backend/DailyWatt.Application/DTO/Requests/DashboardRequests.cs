using System;
using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Application.DTO.Requests;

/// <summary>
/// Request to retrieve time series data with optional weather.
/// Supported granularities: 30min, hour, day, month, year.
/// </summary>
public record GetTimeSeriesRequest
{
  [Required(ErrorMessage = "From date is required")]
  public required DateTime From { get; init; }

  [Required(ErrorMessage = "To date is required")]
  public required DateTime To { get; init; }

  [Required(ErrorMessage = "Granularity is required")]
  [RegularExpression(@"^(30min|hour|day|month|year)$",
    ErrorMessage = "Granularity must be one of: 30min, hour, day, month, year")]
  public required string Granularity { get; init; }

  public bool WithWeather { get; init; }

  public Guid? MeterId { get; init; }
}
