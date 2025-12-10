using DailyWatt.Api.Extensions;
using DailyWatt.Api.Helpers;
using DailyWatt.Application.DTO.Requests;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Application.Services;
using DailyWatt.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyWatt.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardQueryService _dashboardQueryService;

    public DashboardController(IDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    [HttpGet("timeseries")]
    public async Task<ActionResult<TimeSeriesResponse>> GetTimeSeries(
        [FromQuery] GetTimeSeriesRequest request,
        CancellationToken ct = default)
    {
        // Validate date ranges
        var validationError = ValidateDateRanges(request.From, request.To, request.StartDate, request.EndDate);
        if (validationError != null)
        {
            return BadRequest(new { error = validationError });
        }

        var userId = User.GetUserId();
        var granularityValue = GranularityHelper.Parse(request.Granularity);

        // Delegate to service for data composition
        var response = await _dashboardQueryService.GetTimeSeriesAsync(
            userId,
            request.From,
            request.To,
            request.StartDate,
            request.EndDate,
            granularityValue,
            request.WithWeather,
            ct);

        return Ok(response);
    }

    /// <summary>
    /// Validates date ranges for the query.
    /// </summary>
    private static string? ValidateDateRanges(DateTime from, DateTime to, DateTime? startDate, DateTime? endDate)
    {
        if (to <= from)
        {
            return "Invalid date range";
        }

        if (startDate.HasValue && endDate.HasValue && endDate <= startDate)
        {
            return "Invalid date range for query";
        }

        return null;
    }
}
