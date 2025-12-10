using DailyWatt.Domain.Enums;

namespace DailyWatt.Application.Services;

/// <summary>
/// Interface for dashboard data query composition.
/// Handles building the complete TimeSeriesResponse from domain services.
/// </summary>
public interface IDashboardQueryService
{
  /// <summary>
  /// Gets time series data with optional weather information.
  /// </summary>
  Task<TimeSeriesResponse> GetTimeSeriesAsync(
      Guid userId,
      DateTime from,
      DateTime to,
      DateTime? startDate,
      DateTime? endDate,
      Granularity granularity,
      bool withWeather,
      CancellationToken ct = default);
}
