using DailyWatt.Api.Extensions;
using DailyWatt.Api.Models.Enedis;
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

    public EnedisController(IEnedisCredentialService credentialsService, IImportJobService importJobService)
    {
        _credentialsService = credentialsService;
        _importJobService = importJobService;
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> SaveCredentials([FromBody] SaveEnedisCredentialsRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _credentialsService.SaveCredentialsAsync(userId, request.Login, request.Password, ct);
        return Ok();
    }

    [HttpPost("import")]
    public async Task<ActionResult<ImportJobResponse>> CreateImportJob([FromBody] CreateImportJobRequest request, CancellationToken ct)
    {
        if (request.ToUtc <= request.FromUtc)
        {
            return BadRequest(new { error = "Invalid date range" });
        }

        var userId = User.GetUserId();
        var job = await _importJobService.CreateJobAsync(userId, request.FromUtc.ToUniversalTime(), request.ToUtc.ToUniversalTime(), ct);
        return Ok(new ImportJobResponse
        {
            Id = job.Id,
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            ErrorCode = job.ErrorCode,
            ErrorMessage = job.ErrorMessage,
            ImportedCount = job.ImportedCount,
            Status = job.Status
        });
    }

    [HttpGet("import/{jobId:guid}")]
    public async Task<ActionResult<ImportJobResponse>> GetJob(Guid jobId, CancellationToken ct)
    {
        var job = await _importJobService.GetAsync(jobId, ct);
        if (job == null || job.UserId != User.GetUserId())
        {
            return NotFound();
        }

        return Ok(new ImportJobResponse
        {
            Id = job.Id,
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            ErrorCode = job.ErrorCode,
            ErrorMessage = job.ErrorMessage,
            ImportedCount = job.ImportedCount,
            Status = job.Status
        });
    }
}
