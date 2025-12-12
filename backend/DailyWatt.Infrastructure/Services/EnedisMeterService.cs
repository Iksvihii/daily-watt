using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWatt.Infrastructure.Services;

public class EnedisMeterService : IEnedisMeterService
{
  private readonly ApplicationDbContext _db;
  private readonly IWeatherDataService _weatherDataService;
  private readonly IWeatherSyncService _weatherSyncService;
  private readonly IConsumptionService _consumptionService;

  public EnedisMeterService(ApplicationDbContext db, IWeatherDataService weatherDataService, IWeatherSyncService weatherSyncService, IConsumptionService consumptionService)
  {
    _db = db;
    _weatherDataService = weatherDataService;
    _weatherSyncService = weatherSyncService;
    _consumptionService = consumptionService;
  }

  public async Task<IReadOnlyList<EnedisMeter>> GetMetersAsync(Guid userId, CancellationToken ct = default)
  {
    return await _db.EnedisMeters
        .AsNoTracking()
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.IsFavorite)
        .ThenBy(x => x.CreatedAtUtc)
        .ToListAsync(ct);
  }

  public async Task<EnedisMeter?> GetAsync(Guid userId, Guid meterId, CancellationToken ct = default)
  {
    return await _db.EnedisMeters.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Id == meterId, ct);
  }

  public async Task<EnedisMeter?> GetDefaultMeterAsync(Guid userId, CancellationToken ct = default)
  {
    return await _db.EnedisMeters
        .AsNoTracking()
        .Where(x => x.UserId == userId)
        .OrderByDescending(x => x.IsFavorite)
        .ThenBy(x => x.CreatedAtUtc)
        .FirstOrDefaultAsync(ct);
  }

  public async Task<EnedisMeter> CreateAsync(Guid userId, string prm, string? label, string? city, double? latitude, double? longitude, bool isFavorite, CancellationToken ct = default)
  {
    await EnsureUniquePrm(userId, prm, null, ct);

    if (isFavorite)
    {
      await UnfavoriteAll(userId, ct);
    }

    var now = DateTime.UtcNow;
    var meter = new EnedisMeter
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Prm = prm,
      Label = label,
      City = city,
      Latitude = latitude,
      Longitude = longitude,
      IsFavorite = isFavorite,
      CreatedAtUtc = now,
      UpdatedAtUtc = now
    };

    _db.EnedisMeters.Add(meter);
    await _db.SaveChangesAsync(ct);

    return meter;
  }

  public async Task UpdateAsync(Guid userId, Guid meterId, string prm, string? label, string? city, double? latitude, double? longitude, CancellationToken ct = default)
  {
    await EnsureUniquePrm(userId, prm, meterId, ct);

    var meter = await _db.EnedisMeters.FirstOrDefaultAsync(x => x.Id == meterId && x.UserId == userId, ct);
    if (meter == null)
    {
      throw new InvalidOperationException("Meter not found");
    }

    // Check if geolocation has changed
    var geolocationChanged = meter.Latitude != latitude || meter.Longitude != longitude;

    meter.Prm = prm;
    meter.Label = label;
    meter.City = city;
    meter.Latitude = latitude;
    meter.Longitude = longitude;
    meter.UpdatedAtUtc = DateTime.UtcNow;

    _db.EnedisMeters.Update(meter);
    await _db.SaveChangesAsync(ct);

    // If geolocation changed, delete old weather data and regenerate
    if (geolocationChanged && latitude.HasValue && longitude.HasValue)
    {
      await _weatherDataService.DeleteAllAsync(userId, meterId, ct);

      var measurementRange = await _consumptionService.GetMeasurementRangeAsync(userId, meterId, ct);
      if (measurementRange.MinTimestampUtc.HasValue && measurementRange.MaxTimestampUtc.HasValue)
      {
        var dataRangeStart = DateOnly.FromDateTime(measurementRange.MinTimestampUtc.Value);
        var dataRangeEnd = DateOnly.FromDateTime(measurementRange.MaxTimestampUtc.Value);

        await _weatherSyncService.EnsureWeatherAsync(
            userId,
            meterId,
            latitude.Value,
            longitude.Value,
            dataRangeStart,
            dataRangeEnd,
            ct);
      }
    }
  }

  public async Task DeleteAsync(Guid userId, Guid meterId, CancellationToken ct = default)
  {
    var meter = await _db.EnedisMeters.FirstOrDefaultAsync(x => x.Id == meterId && x.UserId == userId, ct);
    if (meter == null)
    {
      return;
    }

    // Remove dependent data
    await _db.Measurements.Where(m => m.UserId == userId && m.MeterId == meterId).ExecuteDeleteAsync(ct);
    await _db.ImportJobs.Where(j => j.UserId == userId && j.MeterId == meterId).ExecuteDeleteAsync(ct);
    await _weatherDataService.DeleteAllAsync(userId, meterId, ct);

    _db.EnedisMeters.Remove(meter);
    await _db.SaveChangesAsync(ct);

    // If it was favorite, assign another as favorite
    if (meter.IsFavorite)
    {
      var next = await _db.EnedisMeters.FirstOrDefaultAsync(x => x.UserId == userId, ct);
      if (next != null)
      {
        next.IsFavorite = true;
        next.UpdatedAtUtc = DateTime.UtcNow;
        _db.EnedisMeters.Update(next);
        await _db.SaveChangesAsync(ct);
      }
    }
  }

  public async Task SetFavoriteAsync(Guid userId, Guid meterId, CancellationToken ct = default)
  {
    var meter = await _db.EnedisMeters.FirstOrDefaultAsync(x => x.Id == meterId && x.UserId == userId, ct);
    if (meter == null)
    {
      throw new InvalidOperationException("Meter not found");
    }

    await UnfavoriteAll(userId, ct);
    meter.IsFavorite = true;
    meter.UpdatedAtUtc = DateTime.UtcNow;
    _db.EnedisMeters.Update(meter);
    await _db.SaveChangesAsync(ct);
  }

  private async Task EnsureUniquePrm(Guid userId, string prm, Guid? excludeMeterId, CancellationToken ct)
  {
    var exists = await _db.EnedisMeters.AnyAsync(x => x.UserId == userId && x.Prm == prm && (!excludeMeterId.HasValue || x.Id != excludeMeterId), ct);
    if (exists)
    {
      throw new InvalidOperationException("PRM already exists for this user");
    }
  }

  private async Task UnfavoriteAll(Guid userId, CancellationToken ct)
  {
    var favorites = await _db.EnedisMeters.Where(x => x.UserId == userId && x.IsFavorite).ToListAsync(ct);
    if (favorites.Count == 0) return;

    foreach (var meter in favorites)
    {
      meter.IsFavorite = false;
      meter.UpdatedAtUtc = DateTime.UtcNow;
    }

    _db.EnedisMeters.UpdateRange(favorites);
    await _db.SaveChangesAsync(ct);
  }
}