using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWatt.Infrastructure.Services;

public class EnedisCredentialService : IEnedisCredentialService
{
    private readonly ApplicationDbContext _db;
    private readonly ISecretProtector _secretProtector;
    private readonly IWeatherDataService _weatherDataService;

    public EnedisCredentialService(ApplicationDbContext db, ISecretProtector secretProtector, IWeatherDataService weatherDataService)
    {
        _db = db;
        _secretProtector = secretProtector;
        _weatherDataService = weatherDataService;
    }

    public async Task SaveCredentialsAsync(
        Guid userId,
        string login,
        string password,
        CancellationToken ct = default)
    {
        var entity = await _db.EnedisCredentials.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (entity == null)
        {
            entity = new EnedisCredential { UserId = userId };
            _db.EnedisCredentials.Add(entity);
        }

        entity.LoginEncrypted = _secretProtector.Protect(login);
        if (!string.IsNullOrEmpty(password))
        {
            entity.PasswordEncrypted = _secretProtector.Protect(password);
        }
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        var credential = await _db.EnedisCredentials.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (credential == null)
        {
            return;
        }

        _db.EnedisCredentials.Remove(credential);

        // Remove meters (cascade removes measurements/weather/imports)
        var meters = await _db.EnedisMeters.Where(m => m.UserId == userId).ToListAsync(ct);
        if (meters.Count > 0)
        {
            _db.EnedisMeters.RemoveRange(meters);
        }

        // Remove cached weather not linked to meter (safety)
        await _weatherDataService.DeleteAllForUserAsync(userId, ct);

        await _db.SaveChangesAsync(ct);
    }

    public Task<EnedisCredential?> GetCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        return _db.EnedisCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }
}
