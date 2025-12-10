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
  private readonly IWeatherProviderService _weatherProviderService;
  private readonly IEnedisCredentialService _enedisCredentialService;
  private readonly IMapper _mapper;

  public DashboardQueryService(
      IConsumptionService consumptionService,
      IWeatherProviderService weatherProviderService,
      IEnedisCredentialService enedisCredentialService,
      IMapper mapper)
  {
    _consumptionService = consumptionService;
    _weatherProviderService = weatherProviderService;
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
        var fromDate = DateOnly.FromDateTime(queryStartDate);
        var toDate = DateOnly.FromDateTime(queryEndDate);

        // Fetch real-time weather data from external provider
        var weatherData = await _weatherProviderService.GetWeatherAsync(
            credentials.Latitude.Value,
            credentials.Longitude.Value,
            fromDate,
            toDate,
            ct);

        // Map to DTOs
        response.Weather = _mapper.Map<List<WeatherDayDto>>(weatherData);
      }
    }

    return response;
  }
}
