using DailyWatt.Api.Extensions;
using DailyWatt.Api.Services;
using DailyWatt.Application.DTO.Requests;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Application.Services;
using DailyWatt.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DailyWatt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Application.Services.IAuthService _authService;
    private readonly IJwtTokenService _tokenService;
    private readonly UserManager<DailyWattUser> _userManager;

    public AuthController(
        Application.Services.IAuthService authService,
        IJwtTokenService tokenService,
        UserManager<DailyWattUser> userManager)
    {
        _authService = authService;
        _tokenService = tokenService;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<ActionResult<string>> Register(RegisterRequest request)
    {
        var (success, errorMessage, user) = await _authService.RegisterAsync(request);
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        var token = _tokenService.CreateToken(user!);
        return Ok(token);
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(LoginRequest request)
    {
        var (success, user) = await _authService.AuthenticateAsync(request);
        if (!success)
        {
            return Unauthorized();
        }

        var token = _tokenService.CreateToken(user!);
        return Ok(token);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var profile = await _authService.GetProfileAsync(user);
        return Ok(profile);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var (success, errorMessage) = await _authService.UpdateProfileAsync(user, request);
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var (success, errorMessage) = await _authService.ChangePasswordAsync(user, request);
        if (!success)
        {
            return BadRequest(new { errors = new[] { errorMessage } });
        }

        return NoContent();
    }
}
