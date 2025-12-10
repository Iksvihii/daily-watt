using DailyWatt.Application.DTO.Requests;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Domain.Entities;

namespace DailyWatt.Application.Services;

/// <summary>
/// Interface for authentication business logic operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<(bool Success, string? ErrorMessage, DailyWattUser? User)> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Authenticates a user and returns the user entity.
    /// </summary>
    Task<(bool Success, DailyWattUser? User)> AuthenticateAsync(
        LoginRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a user's profile information.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(
        DailyWattUser user,
        UpdateProfileRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> ChangePasswordAsync(
        DailyWattUser user,
        ChangePasswordRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets user profile information.
    /// </summary>
    Task<UserProfileDto> GetProfileAsync(
        DailyWattUser user,
        CancellationToken ct = default);
}
