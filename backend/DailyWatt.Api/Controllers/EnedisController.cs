using AutoMapper;
using DailyWatt.Api.Extensions;
using DailyWatt.Application.DTO.Requests;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyWatt.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EnedisController : ControllerBase
{
    private readonly IEnedisCredentialService _credentialsService;
    private readonly IImportJobService _importJobService;
    private readonly IGeocodingService _geocodingService;
    private readonly ISecretProtector _secretProtector;
    private readonly IMapper _mapper;

    public EnedisController(
        IEnedisCredentialService credentialsService,
        IImportJobService importJobService,
        IGeocodingService geocodingService,
        ISecretProtector secretProtector,
        IMapper mapper)
    {
        _credentialsService = credentialsService;
        _importJobService = importJobService;
        _geocodingService = geocodingService;
        _secretProtector = secretProtector;
        _mapper = mapper;
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> SaveCredentials([FromBody] SaveEnedisCredentialsRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _credentialsService.SaveCredentialsAsync(
            userId,
            request.Login,
            request.Password,
            request.MeterNumber,
            request.City,
            request.Latitude,
            request.Longitude,
            ct);
        return Ok();
    }

    [HttpGet("credentials")]
    public async Task<ActionResult<dynamic>> GetCredentials(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var cred = await _credentialsService.GetCredentialsAsync(userId, ct);

        if (cred == null)
        {
            return NotFound();
        }

        var decryptedLogin = cred.LoginEncrypted != null && cred.LoginEncrypted.Length > 0
            ? _secretProtector.Unprotect(cred.LoginEncrypted)
            : "";

        return Ok(new
        {
            login = decryptedLogin,
            meterNumber = cred.MeterNumber,
            city = cred.City,
            latitude = cred.Latitude,
            longitude = cred.Longitude,
            updatedAt = cred.UpdatedAt
        });
    }

    [HttpGet("status")]
    public async Task<ActionResult<EnedisStatusResponse>> GetStatus(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var cred = await _credentialsService.GetCredentialsAsync(userId, ct);

        if (cred == null)
        {
            return Ok(new EnedisStatusResponse { Configured = false });
        }

        return Ok(new EnedisStatusResponse
        {
            Configured = true,
            MeterNumber = cred.MeterNumber,
            UpdatedAt = cred.UpdatedAt
        });
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportJobDto>> CreateImportJob([FromBody] CreateImportJobRequest request, CancellationToken ct)
    {
        if (request.ToUtc <= request.FromUtc)
        {
            return BadRequest(new { error = "Invalid date range" });
        }

        var userId = User.GetUserId();
        var job = await _importJobService.CreateJobAsync(userId, request.FromUtc.ToUniversalTime(), request.ToUtc.ToUniversalTime(), ct);
        var dto = _mapper.Map<ImportJobDto>(job);

        return Ok(dto);
    }

    [HttpGet("import/{jobId:guid}")]
    public async Task<ActionResult<ImportJobDto>> GetJob(Guid jobId, CancellationToken ct)
    {
        var job = await _importJobService.GetAsync(jobId, ct);
        if (job == null || job.UserId != User.GetUserId())
        {
            return NotFound();
        }

        var dto = _mapper.Map<ImportJobDto>(job);

        return Ok(dto);
    }

    [HttpGet("geocode/suggestions")]
    public async Task<ActionResult<List<string>>> GetCitySuggestions(
        [FromQuery] string query,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return BadRequest(new { error = "Query must be at least 2 characters" });
        }

        var suggestions = await _geocodingService.GetCitySuggestionsAsync(query, ct);
        return Ok(suggestions);
    }

    [HttpPost("geocode")]
    public async Task<ActionResult<dynamic>> GeocodeCity([FromBody] dynamic request, CancellationToken ct)
    {
        var city = ((System.Text.Json.JsonElement)request).GetProperty("city").GetString();

        if (string.IsNullOrWhiteSpace(city))
        {
            return BadRequest(new { error = "City is required" });
        }

        var result = await _geocodingService.GeocodeAsync(city, ct);

        if (result == null)
        {
            return NotFound(new { error = "City not found" });
        }

        return Ok(new { latitude = result.Value.latitude, longitude = result.Value.longitude });
    }
}

