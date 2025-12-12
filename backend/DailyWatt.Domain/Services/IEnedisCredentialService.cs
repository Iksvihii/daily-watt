using DailyWatt.Domain.Entities;

namespace DailyWatt.Domain.Services;

public interface IEnedisCredentialService
{
    Task SaveCredentialsAsync(
        Guid userId,
        string login,
        string password,
        CancellationToken ct = default);

    Task<EnedisCredential?> GetCredentialsAsync(Guid userId, CancellationToken ct = default);

    Task DeleteCredentialsAsync(Guid userId, CancellationToken ct = default);
}
