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
  private readonly IWeatherSyncService _weatherSyncService;
  private readonly IWeatherDataService _weatherDataService;
  private readonly IEnedisMeterService _enedisMeterService;
  private readonly IMapper _mapper;

  public DashboardQueryService(
      IConsumptionService consumptionService,
      IWeatherSyncService weatherSyncService,
      IWeatherDataService weatherDataService,
      IEnedisMeterService enedisMeterService,
      IMapper mapper)
  {
    _consumptionService = consumptionService;
    _weatherSyncService = weatherSyncService;
    _weatherDataService = weatherDataService;
    _enedisMeterService = enedisMeterService;
    _mapper = mapper;
  }

  public async Task<TimeSeriesResponse> GetTimeSeriesAsync(
      Guid userId,
      Guid? meterId,
      DateTime from,
      DateTime to,
      Granularity granularity,
      bool withWeather,
      CancellationToken ct = default)
  {
    var queryStartDate = from.ToUniversalTime();
    var queryEndDate = to.ToUniversalTime();

    var meter = meterId.HasValue
      ? await _enedisMeterService.GetAsync(userId, meterId.Value, ct)
      : await _enedisMeterService.GetDefaultMeterAsync(userId, ct);

    if (meter == null)
    {
      return new TimeSeriesResponse();
    }

    // Fetch aggregated consumption data
    var consumption = await _consumptionService.GetAggregatedAsync(
        userId,
        meter.Id,
        queryStartDate,
        queryEndDate,
        granularity,
        ct);

    // Fetch summary statistics
    var summary = await _consumptionService.GetSummaryAsync(
        userId,
        meter.Id,
        queryStartDate,
        queryEndDate,
        ct);

    // Map to DTOs using AutoMapper
    var response = new TimeSeriesResponse
    {
      Consumption = _mapper.Map<List<ConsumptionPointDto>>(consumption),
      Summary = _mapper.Map<SummaryDto>(summary)
    };

    // Optionally fetch weather data and ensure sync
    if (withWeather && meter.Latitude.HasValue && meter.Longitude.HasValue)
    {
      // Determine the weather range to ensure (based on measurement range if available)
      var (minTs, maxTs) = await _consumptionService.GetMeasurementRangeAsync(userId, meter.Id, ct);
      var fromDate = DateOnly.FromDateTime(minTs ?? queryStartDate);
      var toDate = DateOnly.FromDateTime(maxTs ?? queryEndDate);

      await _weatherSyncService.EnsureWeatherAsync(
          userId,
          meter.Id,
          meter.Latitude.Value,
          meter.Longitude.Value,
          fromDate,
          toDate,
          ct);

      var requestedFrom = DateOnly.FromDateTime(queryStartDate);
      var requestedTo = DateOnly.FromDateTime(queryEndDate);
      var weatherForResponse = await _weatherDataService.GetAsync(userId, meter.Id, requestedFrom, requestedTo, ct);
      response.Weather = _mapper.Map<List<WeatherDayDto>>(weatherForResponse);
    }

    return response;
  }
}
