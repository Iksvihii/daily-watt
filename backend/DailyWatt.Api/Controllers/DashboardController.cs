using DailyWatt.Api.Extensions;
using DailyWatt.Api.Helpers;
using DailyWatt.Api.Models.Dashboard;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyWatt.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IConsumptionService _consumptionService;
    private readonly IWeatherService _weatherService;

    public DashboardController(IConsumptionService consumptionService, IWeatherService weatherService)
    {
        _consumptionService = consumptionService;
        _weatherService = weatherService;
    }

    [HttpGet("timeseries")]
    public async Task<ActionResult<TimeSeriesResponse>> GetTimeSeries(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? granularity,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] bool withWeather = false,
        CancellationToken ct = default)
    {
        if (to <= from)
        {
            return BadRequest(new { error = "Invalid date range" });
        }

        var userId = User.GetUserId();
        var granularityValue = GranularityHelper.Parse(granularity);

        // Use provided range if available, otherwise use full range
        var queryStartDate = (startDate ?? from).ToUniversalTime();
        var queryEndDate = (endDate ?? to).ToUniversalTime();

        if (queryEndDate <= queryStartDate)
        {
            return BadRequest(new { error = "Invalid date range for query" });
        }

        var consumption = await _consumptionService.GetAggregatedAsync(userId, queryStartDate, queryEndDate, granularityValue, ct);
        var summary = await _consumptionService.GetSummaryAsync(userId, queryStartDate, queryEndDate, ct);

        var response = new TimeSeriesResponse
        {
            Consumption = consumption
                .Select(c => new ConsumptionPointDto { TimestampUtc = c.TimestampUtc, Kwh = c.Kwh })
                .ToList(),
            Summary = new SummaryDto
            {
                TotalKwh = summary.TotalKwh,
                AvgKwhPerDay = summary.AvgKwhPerDay,
                MaxDay = summary.MaxDay,
                MaxDayKwh = summary.MaxDayKwh
            }
        };

        if (withWeather)
        {
            var fromDate = DateOnly.FromDateTime(queryStartDate);
            var toDate = DateOnly.FromDateTime(queryEndDate);
            await _weatherService.EnsureWeatherRangeAsync(userId, fromDate, toDate, ct);
            var weather = await _weatherService.GetRangeAsync(userId, fromDate, toDate, ct);
            response.Weather = weather
                .Select(w => new WeatherDayDto
                {
                    Date = w.Date,
                    TempAvg = w.TempAvg,
                    TempMin = w.TempMin,
                    TempMax = w.TempMax,
                    Source = w.Source
                })
                .ToList();
        }

        return Ok(response);
    }
}
