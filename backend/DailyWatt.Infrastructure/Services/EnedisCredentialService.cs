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
        string meterNumber,
        string? city = null,
        double? latitude = null,
        double? longitude = null,
        CancellationToken ct = default)
    {
        var entity = await _db.EnedisCredentials.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var locationChanged = entity != null &&
            (entity.City != city || !Nullable.Equals(entity.Latitude, latitude) || !Nullable.Equals(entity.Longitude, longitude));
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
        entity.MeterNumber = meterNumber;
        entity.City = city;
        entity.Latitude = latitude;
        entity.Longitude = longitude;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (locationChanged)
        {
            await _weatherDataService.DeleteAllAsync(userId, ct);
        }
    }

    public async Task DeleteCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        var credential = await _db.EnedisCredentials.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (credential == null)
        {
            return;
        }

        _db.EnedisCredentials.Remove(credential);

        // Remove related measurements for this user (single meter per user)
        await _db.Measurements.Where(m => m.UserId == userId).ExecuteDeleteAsync(ct);

        // Remove cached weather
        await _weatherDataService.DeleteAllAsync(userId, ct);

        await _db.SaveChangesAsync(ct);
    }

    public Task<EnedisCredential?> GetCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        return _db.EnedisCredentials.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
    }
}
