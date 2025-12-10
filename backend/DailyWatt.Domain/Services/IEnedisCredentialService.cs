using DailyWatt.Domain.Entities;

namespace DailyWatt.Domain.Services;

public interface IEnedisCredentialService
{
    Task SaveCredentialsAsync(
        Guid userId,
        string login,
        string password,
        string meterNumber,
        string? address = null,
        double? latitude = null,
        double? longitude = null,
        CancellationToken ct = default);

    Task<EnedisCredential?> GetCredentialsAsync(Guid userId, CancellationToken ct = default);
}
