using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyWatt.Api.Controllers;

/// <summary>
/// Health check endpoint for frontend connectivity verification
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
  /// <summary>
  /// Simple health check endpoint to verify backend is running
  /// </summary>
  /// <returns>HTTP 200 if backend is available</returns>
  [HttpGet]
  public IActionResult Get()
  {
    return Ok("OK");
  }
}
