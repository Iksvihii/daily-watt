using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// EF Core-backed implementation for persisting cached weather days.
/// </summary>
public class WeatherDataService : IWeatherDataService
{
  private readonly ApplicationDbContext _db;

  public WeatherDataService(ApplicationDbContext db)
  {
    _db = db;
  }

  public async Task<IReadOnlyList<WeatherDay>> GetAsync(Guid userId, Guid meterId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
  {
    return await _db.WeatherDays
        .AsNoTracking()
        .Where(x => x.UserId == userId && x.MeterId == meterId && x.Date >= fromDate && x.Date <= toDate)
        .OrderBy(x => x.Date)
        .ToListAsync(ct);
  }

  public async Task UpsertAsync(Guid userId, Guid meterId, IEnumerable<WeatherDay> weatherDays, CancellationToken ct = default)
  {
    var dayList = weatherDays.ToList();
    if (dayList.Count == 0)
    {
      return;
    }

    var dates = dayList.Select(x => x.Date).ToHashSet();

    var existing = await _db.WeatherDays
        .Where(x => x.UserId == userId && x.MeterId == meterId && dates.Contains(x.Date))
        .ToListAsync(ct);

    if (existing.Count > 0)
    {
      _db.WeatherDays.RemoveRange(existing);
    }

    await _db.WeatherDays.AddRangeAsync(dayList, ct);
    await _db.SaveChangesAsync(ct);
  }

  public async Task DeleteAllAsync(Guid userId, Guid meterId, CancellationToken ct = default)
  {
    var records = await _db.WeatherDays.Where(x => x.UserId == userId && x.MeterId == meterId).ToListAsync(ct);
    if (records.Count == 0)
    {
      return;
    }

    _db.WeatherDays.RemoveRange(records);
    await _db.SaveChangesAsync(ct);
  }

  public async Task DeleteAllForUserAsync(Guid userId, CancellationToken ct = default)
  {
    var records = await _db.WeatherDays.Where(x => x.UserId == userId).ToListAsync(ct);
    if (records.Count == 0)
    {
      return;
    }

    _db.WeatherDays.RemoveRange(records);
    await _db.SaveChangesAsync(ct);
  }
}