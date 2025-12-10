using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Models;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWatt.Infrastructure.Services;

public class ConsumptionService : IConsumptionService
{
    private readonly ApplicationDbContext _db;

    public ConsumptionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Measurement>> GetMeasurementsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        return await _db.Measurements
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc)
            .OrderBy(x => x.TimestampUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AggregatedConsumptionPoint>> GetAggregatedAsync(Guid userId, DateTime fromUtc, DateTime toUtc, Granularity granularity, CancellationToken ct = default)
    {
        var query = _db.Measurements.AsNoTracking()
            .Where(x => x.UserId == userId && x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc);

        switch (granularity)
        {
            case Granularity.ThirtyMinutes:
                return await query
                    .OrderBy(x => x.TimestampUtc)
                    .Select(x => new AggregatedConsumptionPoint(x.TimestampUtc, x.Kwh))
                    .ToListAsync(ct);

            case Granularity.Hour:
                var hourData = await query.ToListAsync(ct);
                return hourData
                    .GroupBy(x => new DateTime(x.TimestampUtc.Year, x.TimestampUtc.Month, x.TimestampUtc.Day, x.TimestampUtc.Hour, 0, 0, DateTimeKind.Utc))
                    .Select(g => new AggregatedConsumptionPoint(g.Key, g.Sum(m => m.Kwh)))
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();

            case Granularity.Day:
                var dayData = await query.ToListAsync(ct);
                return dayData
                    .GroupBy(x => new DateTime(x.TimestampUtc.Year, x.TimestampUtc.Month, x.TimestampUtc.Day, 0, 0, 0, DateTimeKind.Utc))
                    .Select(g => new AggregatedConsumptionPoint(g.Key, g.Sum(m => m.Kwh)))
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();

            case Granularity.Month:
                var monthData = await query.ToListAsync(ct);
                return monthData
                    .GroupBy(x => new DateTime(x.TimestampUtc.Year, x.TimestampUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc))
                    .Select(g => new AggregatedConsumptionPoint(g.Key, g.Sum(m => m.Kwh)))
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();

            case Granularity.Year:
                var yearData = await query.ToListAsync(ct);
                return yearData
                    .GroupBy(x => new DateTime(x.TimestampUtc.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .Select(g => new AggregatedConsumptionPoint(g.Key, g.Sum(m => m.Kwh)))
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();

            default:
                return await query
                    .GroupBy(x => new DateTime(x.TimestampUtc.Year, x.TimestampUtc.Month, x.TimestampUtc.Day, 0, 0, 0, DateTimeKind.Utc))
                    .Select(g => new AggregatedConsumptionPoint(g.Key, g.Sum(m => m.Kwh)))
                    .OrderBy(x => x.TimestampUtc)
                    .ToListAsync(ct);
        }
    }

    public async Task<ConsumptionSummary> GetSummaryAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var daily = await _db.Measurements.AsNoTracking()
            .Where(x => x.UserId == userId && x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc)
            .GroupBy(x => new DateTime(x.TimestampUtc.Year, x.TimestampUtc.Month, x.TimestampUtc.Day, 0, 0, 0, DateTimeKind.Utc))
            .Select(g => new { Date = g.Key, Total = g.Sum(m => m.Kwh) })
            .ToListAsync(ct);

        var total = daily.Sum(x => x.Total);
        var avg = daily.Count == 0 ? 0 : daily.Average(x => x.Total);
        var maxDay = daily.OrderByDescending(x => x.Total).FirstOrDefault();
        return new ConsumptionSummary(total, avg, maxDay?.Total ?? 0, maxDay != null ? DateOnly.FromDateTime(maxDay.Date) : null);
    }

    public async Task BulkInsertAsync(IEnumerable<Measurement> measurements, CancellationToken ct = default)
    {
        await _db.Measurements.AddRangeAsync(measurements, ct);
        await _db.SaveChangesAsync(ct);
    }
}
