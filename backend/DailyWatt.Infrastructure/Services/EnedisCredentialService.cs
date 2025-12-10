using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWatt.Infrastructure.Services;

public class EnedisCredentialService : IEnedisCredentialService
{
    private readonly ApplicationDbContext _db;
    private readonly ISecretProtector _secretProtector;

    public EnedisCredentialService(ApplicationDbContext db, ISecretProtector secretProtector)
    {
        _db = db;
        _secretProtector = secretProtector;
    }

    public async Task SaveCredentialsAsync(
        Guid userId,
        string login,
        string password,
        string meterNumber,
        string? address = null,
        double? latitude = null,
        double? longitude = null,
        CancellationToken ct = default)
    {
        var entity = await _db.EnedisCredentials.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (entity == null)
        {
            entity = new EnedisCredential { UserId = userId };
            _db.EnedisCredentials.Add(entity);
        }

        entity.LoginEncrypted = _secretProtector.Protect(login);
        entity.PasswordEncrypted = _secretProtector.Protect(password);
        entity.MeterNumber = meterNumber;
        entity.Address = address;
        entity.Latitude = latitude;
        entity.Longitude = longitude;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public Task<EnedisCredential?> GetCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        return _db.EnedisCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }
}
