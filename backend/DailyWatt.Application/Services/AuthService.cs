using DailyWatt.Application.DTO.Requests;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DailyWatt.Application.Services;

/// <summary>
/// Service handling authentication business logic.
/// Decouples auth logic from the controller layer.
/// </summary>
public class AuthService : IAuthService
{
  private readonly UserManager<DailyWattUser> _userManager;

  public AuthService(UserManager<DailyWattUser> userManager)
  {
    _userManager = userManager;
  }

  public async Task<(bool Success, string? ErrorMessage, DailyWattUser? User)> RegisterAsync(
      RegisterRequest request,
      CancellationToken ct = default)
  {
    // Check if email already registered
    var existing = await _userManager.FindByEmailAsync(request.Email);
    if (existing != null)
    {
      return (false, "Email already registered.", null);
    }

    // Check if username already taken
    var usernameExists = await _userManager.FindByNameAsync(request.Username);
    if (usernameExists != null)
    {
      return (false, "Username already taken.", null);
    }

    // Create new user
    var user = new DailyWattUser
    {
      Id = Guid.NewGuid(),
      Email = request.Email,
      UserName = request.Username
    };

    var result = await _userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
    {
      var errors = string.Join(", ", result.Errors.Select(e => e.Description));
      return (false, errors, null);
    }

    return (true, null, user);
  }

  public async Task<(bool Success, DailyWattUser? User)> AuthenticateAsync(
      LoginRequest request,
      CancellationToken ct = default)
  {
    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user == null)
    {
      return (false, null);
    }

    var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
    if (!passwordValid)
    {
      return (false, null);
    }

    return (true, user);
  }

  public async Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(
      DailyWattUser user,
      UpdateProfileRequest request,
      CancellationToken ct = default)
  {
    // Check if new username is already taken by another user
    var usernameExists = await _userManager.FindByNameAsync(request.Username);
    if (usernameExists != null && usernameExists.Id != user.Id)
    {
      return (false, "Username already taken.");
    }

    user.UserName = request.Username;
    var result = await _userManager.UpdateAsync(user);

    if (!result.Succeeded)
    {
      var errors = string.Join(", ", result.Errors.Select(e => e.Description));
      return (false, errors);
    }

    return (true, null);
  }

  public async Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
      DailyWattUser user,
      ChangePasswordRequest request,
      CancellationToken ct = default)
  {
    var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

    if (!result.Succeeded)
    {
      var errors = string.Join(", ", result.Errors.Select(e => e.Description));
      return (false, errors);
    }

    return (true, null);
  }

  public async Task<UserProfileDto> GetProfileAsync(
      DailyWattUser user,
      CancellationToken ct = default)
  {
    return new UserProfileDto
    {
      Email = user.Email ?? string.Empty,
      Username = user.UserName ?? string.Empty
    };
  }
}
