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
    private readonly IMapper _mapper;

    public EnedisController(
        IEnedisCredentialService credentialsService,
        IImportJobService importJobService,
        IMapper mapper)
    {
        _credentialsService = credentialsService;
        _importJobService = importJobService;
        _mapper = mapper;
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> SaveCredentials([FromBody] SaveEnedisCredentialsRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        await _credentialsService.SaveCredentialsAsync(userId, request.Login, request.Password, request.MeterNumber, ct);
        return Ok();
    }

    [HttpGet("status")]
    public async Task<ActionResult<EnedisStatus>> GetStatus(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var cred = await _credentialsService.GetCredentialsAsync(userId, ct);

        if (cred == null)
        {
            return Ok(new EnedisStatus { Configured = false });
        }

        return Ok(new EnedisStatus
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
}

