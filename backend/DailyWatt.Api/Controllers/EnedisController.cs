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
    private readonly IEnedisMeterService _meterService;
    private readonly IImportJobService _importJobService;
    private readonly IGeocodingService _geocodingService;
    private readonly ISecretProtector _secretProtector;
    private readonly IMapper _mapper;
    private readonly ILogger<EnedisController> _logger;

    public EnedisController(
        IEnedisCredentialService credentialsService,
        IEnedisMeterService meterService,
        IImportJobService importJobService,
        IGeocodingService geocodingService,
        ISecretProtector secretProtector,
        IMapper mapper,
        ILogger<EnedisController> logger)
    {
        _credentialsService = credentialsService;
        _meterService = meterService;
        _importJobService = importJobService;
        _geocodingService = geocodingService;
        _secretProtector = secretProtector;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> SaveCredentials([FromBody] SaveEnedisCredentialsRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _credentialsService.SaveCredentialsAsync(
            userId,
            request.Login,
            request.Password,
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

        string decryptedLogin = "";
        try
        {
            decryptedLogin = cred.LoginEncrypted != null && cred.LoginEncrypted.Length > 0
                ? _secretProtector.Unprotect(cred.LoginEncrypted)
                : "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt login for user {UserId}", userId);
            // Return empty login if decryption fails
        }

        return Ok(new
        {
            login = decryptedLogin,
            updatedAt = cred.UpdatedAt
        });
    }

    [HttpDelete("credentials")]
    public async Task<IActionResult> DeleteCredentials(CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _credentialsService.DeleteCredentialsAsync(userId, ct);
        return NoContent();
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
            UpdatedAt = cred.UpdatedAt
        });
    }

    // Meter CRUD endpoints
    [HttpGet("meters")]
    public async Task<ActionResult<List<dynamic>>> GetMeters(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var meters = await _meterService.GetMetersAsync(userId, ct);

        var result = meters.Select(m => new
        {
            id = m.Id,
            prm = m.Prm,
            label = m.Label,
            city = m.City,
            latitude = m.Latitude,
            longitude = m.Longitude,
            isFavorite = m.IsFavorite,
            createdAt = m.CreatedAtUtc,
            updatedAt = m.UpdatedAtUtc
        }).ToList();

        return Ok(result);
    }

    [HttpPost("meters")]
    public async Task<ActionResult<dynamic>> CreateMeter(
        [FromBody] CreateMeterRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        try
        {
            var meter = await _meterService.CreateAsync(
                userId,
                request.Prm,
                request.Label,
                request.City,
                request.Latitude,
                request.Longitude,
                request.IsFavorite,
                ct);

            return Ok(new
            {
                id = meter.Id,
                prm = meter.Prm,
                label = meter.Label,
                city = meter.City,
                latitude = meter.Latitude,
                longitude = meter.Longitude,
                isFavorite = meter.IsFavorite,
                createdAt = meter.CreatedAtUtc,
                updatedAt = meter.UpdatedAtUtc
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("meters/{meterId:guid}")]
    public async Task<IActionResult> UpdateMeter(
        Guid meterId,
        [FromBody] UpdateMeterRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        try
        {
            await _meterService.UpdateAsync(
                userId,
                meterId,
                request.Prm,
                request.Label,
                request.City,
                request.Latitude,
                request.Longitude,
                ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("meters/{meterId:guid}")]
    public async Task<IActionResult> DeleteMeter(Guid meterId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _meterService.DeleteAsync(userId, meterId, ct);
        return NoContent();
    }

    [HttpPost("meters/{meterId:guid}/favorite")]
    public async Task<IActionResult> SetFavoriteMeter(Guid meterId, CancellationToken ct)
    {
        var userId = User.GetUserId();

        try
        {
            await _meterService.SetFavoriteAsync(userId, meterId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportJobDto>> CreateImportJob([FromBody] CreateImportJobRequest request, CancellationToken ct)
    {
        if (request.ToUtc <= request.FromUtc)
        {
            return BadRequest(new { error = "Invalid date range" });
        }

        var userId = User.GetUserId();
        var job = await _importJobService.CreateJobAsync(userId, request.MeterId, request.FromUtc.ToUniversalTime(), request.ToUtc.ToUniversalTime(), ct);
        var dto = _mapper.Map<ImportJobDto>(job);

        return Ok(dto);
    }

    [HttpPost("import/upload")]
    public async Task<ActionResult<ImportJobDto>> UploadConsumptionFile(
        [FromForm] IFormFile file,
        [FromForm] Guid meterId,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File is required" });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            return BadRequest(new { error = "Only Excel files (.xlsx, .xls) are supported" });
        }

        var userId = User.GetUserId();

        // Save file to temp directory
        var uploadsDir = Path.Combine(Path.GetTempPath(), "dailywatt-uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{userId}_{meterId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        _logger.LogInformation("Uploaded file saved to {FilePath} for user {UserId}", filePath, userId);

        // Create import job with file path (dates will be extracted from Excel)
        var job = await _importJobService.CreateJobWithFileAsync(
            userId,
            meterId,
            filePath,
            ct);

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

    [HttpPost("reverse-geocode")]
    public async Task<ActionResult<dynamic>> ReverseGeocodeCoordinates([FromBody] dynamic request, CancellationToken ct)
    {
        try
        {
            var element = (System.Text.Json.JsonElement)request;
            if (!double.TryParse(element.GetProperty("latitude").ToString(), System.Globalization.CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(element.GetProperty("longitude").ToString(), System.Globalization.CultureInfo.InvariantCulture, out var longitude))
            {
                return BadRequest(new { error = "Invalid latitude or longitude" });
            }

            var cityName = await _geocodingService.ReverseGeocodeAsync(latitude, longitude, ct);

            if (string.IsNullOrWhiteSpace(cityName))
            {
                return NotFound(new { error = "City not found for these coordinates" });
            }

            return Ok(new { city = cityName });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Invalid request format", details = ex.Message });
        }
    }
}

