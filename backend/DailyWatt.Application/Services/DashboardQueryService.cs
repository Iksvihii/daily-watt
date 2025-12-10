using AutoMapper;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Services;

namespace DailyWatt.Application.Services;

/// <summary>
/// Service for composing dashboard queries.
/// Orchestrates calls to domain services and maps results to DTOs.
/// </summary>
public class DashboardQueryService : IDashboardQueryService
{
  private readonly IConsumptionService _consumptionService;
  private readonly IWeatherService _weatherService;
  private readonly IMapper _mapper;

  public DashboardQueryService(
      IConsumptionService consumptionService,
      IWeatherService weatherService,
      IMapper mapper)
  {
    _consumptionService = consumptionService;
    _weatherService = weatherService;
    _mapper = mapper;
  }

  public async Task<TimeSeriesResponse> GetTimeSeriesAsync(
      Guid userId,
      DateTime from,
      DateTime to,
      DateTime? startDate,
      DateTime? endDate,
      Granularity granularity,
      bool withWeather,
      CancellationToken ct = default)
  {
    // Use provided range if available, otherwise use full range
    var queryStartDate = (startDate ?? from).ToUniversalTime();
    var queryEndDate = (endDate ?? to).ToUniversalTime();

    // Fetch aggregated consumption data
    var consumption = await _consumptionService.GetAggregatedAsync(
        userId,
        queryStartDate,
        queryEndDate,
        granularity,
        ct);

    // Fetch summary statistics
    var summary = await _consumptionService.GetSummaryAsync(
        userId,
        queryStartDate,
        queryEndDate,
        ct);

    // Map to DTOs using AutoMapper
    var response = new TimeSeriesResponse
    {
      Consumption = _mapper.Map<List<ConsumptionPointDto>>(consumption),
      Summary = _mapper.Map<SummaryDto>(summary)
    };

    // Optionally fetch weather data
    if (withWeather)
    {
      var fromDate = DateOnly.FromDateTime(queryStartDate);
      var toDate = DateOnly.FromDateTime(queryEndDate);

      // Ensure weather data exists for the range
      await _weatherService.EnsureWeatherRangeAsync(userId, fromDate, toDate, ct);

      // Fetch weather data
      var weather = await _weatherService.GetRangeAsync(userId, fromDate, toDate, ct);

      // Map to DTOs
      response.Weather = _mapper.Map<List<WeatherDayDto>>(weather);
    }

    return response;
  }
}
