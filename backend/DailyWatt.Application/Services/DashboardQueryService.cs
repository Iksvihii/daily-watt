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
  private readonly IEnedisCredentialService _enedisCredentialService;
  private readonly IMapper _mapper;

  public DashboardQueryService(
      IConsumptionService consumptionService,
      IWeatherSyncService weatherSyncService,
      IWeatherDataService weatherDataService,
      IEnedisCredentialService enedisCredentialService,
      IMapper mapper)
  {
    _consumptionService = consumptionService;
    _weatherSyncService = weatherSyncService;
    _weatherDataService = weatherDataService;
    _enedisCredentialService = enedisCredentialService;
    _mapper = mapper;
  }

  public async Task<TimeSeriesResponse> GetTimeSeriesAsync(
      Guid userId,
      DateTime from,
      DateTime to,
      Granularity granularity,
      bool withWeather,
      CancellationToken ct = default)
  {
    var queryStartDate = from.ToUniversalTime();
    var queryEndDate = to.ToUniversalTime();

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

    // Optionally fetch weather data from external provider
    if (withWeather)
    {
      var credentials = await _enedisCredentialService.GetCredentialsAsync(userId, ct);

      if (credentials?.Latitude.HasValue == true && credentials.Longitude.HasValue == true)
      {
        var measurementRange = await _consumptionService.GetMeasurementRangeAsync(userId, ct);

        if (measurementRange.MinTimestampUtc.HasValue && measurementRange.MaxTimestampUtc.HasValue)
        {
          var dataRangeStart = DateOnly.FromDateTime(measurementRange.MinTimestampUtc.Value);
          var dataRangeEnd = DateOnly.FromDateTime(measurementRange.MaxTimestampUtc.Value);

          await _weatherSyncService.EnsureWeatherAsync(
              userId,
              credentials.Latitude.Value,
              credentials.Longitude.Value,
              dataRangeStart,
              dataRangeEnd,
              ct);

          var requestedFrom = DateOnly.FromDateTime(queryStartDate);
          var requestedTo = DateOnly.FromDateTime(queryEndDate);
          var weatherForResponse = await _weatherDataService.GetAsync(userId, requestedFrom, requestedTo, ct);

          response.Weather = _mapper.Map<List<WeatherDayDto>>(weatherForResponse);
        }
      }
    }

    return response;
  }
}
