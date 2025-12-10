using AutoMapper;
using DailyWatt.Api.Extensions;
using DailyWatt.Api.Models.Auth;
using DailyWatt.Api.Services;
using DailyWatt.Application.DTOs;
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
    private readonly IMapper _mapper;
    private readonly UserManager<DailyWattUser> _userManager;

    public AuthController(
        Application.Services.IAuthService authService,
        IJwtTokenService tokenService,
        IMapper mapper,
        UserManager<DailyWattUser> userManager)
    {
        _authService = authService;
        _tokenService = tokenService;
        _mapper = mapper;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Application.DTOs.AuthResponse>> Register(Models.Auth.RegisterRequest request)
    {
        var appRequest = new Application.Services.RegisterRequest(request.Email, request.Username, request.Password);
        var (success, errorMessage, user) = await _authService.RegisterAsync(appRequest);
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        var token = _tokenService.CreateToken(user!);
        return Ok(new Application.DTOs.AuthResponse { Token = token });
    }

    [HttpPost("login")]
    public async Task<ActionResult<Application.DTOs.AuthResponse>> Login(Models.Auth.LoginRequest request)
    {
        var appRequest = new Application.Services.LoginRequest(request.Email, request.Password);
        var (success, user) = await _authService.AuthenticateAsync(appRequest);
        if (!success)
        {
            return Unauthorized();
        }

        var token = _tokenService.CreateToken(user!);
        return Ok(new Application.DTOs.AuthResponse { Token = token });
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

        return Ok(_mapper.Map<UserProfileDto>(user));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(Models.Auth.UpdateProfileRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var appRequest = new Application.Services.UpdateProfileRequest(request.Username);
        var (success, errorMessage) = await _authService.UpdateProfileAsync(user, appRequest);
        if (!success)
        {
            return BadRequest(new { error = errorMessage });
        }

        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(Models.Auth.ChangePasswordRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var appRequest = new Application.Services.ChangePasswordRequest(request.CurrentPassword, request.NewPassword);
        var (success, errorMessage) = await _authService.ChangePasswordAsync(user, appRequest);
        if (!success)
        {
            return BadRequest(new { errors = new[] { errorMessage } });
        }

        return NoContent();
    }
}
