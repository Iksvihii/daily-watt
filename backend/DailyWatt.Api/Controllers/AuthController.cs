using DailyWatt.Api.Models.Auth;
using DailyWatt.Api.Services;
using DailyWatt.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DailyWatt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<DailyWattUser> _userManager;
    private readonly IJwtTokenService _tokenService;

    public AuthController(UserManager<DailyWattUser> userManager, IJwtTokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            return BadRequest(new { error = "Email already registered." });
        }

        var usernameExists = await _userManager.FindByNameAsync(request.Username);
        if (usernameExists != null)
        {
            return BadRequest(new { error = "Username already taken." });
        }

        var user = new DailyWattUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Username
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthResponse { Token = token });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Unauthorized();
        }

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthResponse { Token = token });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new UserProfileResponse
        {
            Email = user.Email ?? string.Empty,
            Username = user.UserName ?? string.Empty
        });
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

        var usernameExists = await _userManager.FindByNameAsync(request.Username);
        if (usernameExists != null && usernameExists.Id != user.Id)
        {
            return BadRequest(new { error = "Username already taken." });
        }

        user.UserName = request.Username;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
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

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        return NoContent();
    }
}
