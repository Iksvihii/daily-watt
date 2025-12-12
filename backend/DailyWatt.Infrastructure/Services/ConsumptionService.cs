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

    public async Task<IReadOnlyList<Measurement>> GetMeasurementsAsync(Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        return await _db.Measurements
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.MeterId == meterId && x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc)
            .OrderBy(x => x.TimestampUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AggregatedConsumptionPoint>> GetAggregatedAsync(Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc, Granularity granularity, CancellationToken ct = default)
    {
        var query = _db.Measurements.AsNoTracking()
            .Where(x => x.UserId == userId && x.MeterId == meterId && x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc);

        switch (granularity)
        {
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
                // Default to day aggregation
                var defData = await query.ToListAsync(ct);
                return defData
                    .GroupBy(x => new DateTime(x.TimestampUtc.Year, x.TimestampUtc.Month, x.TimestampUtc.Day, 0, 0, 0, DateTimeKind.Utc))
                    .Select(g => new AggregatedConsumptionPoint(g.Key, g.Sum(m => m.Kwh)))
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();
        }
    }

    public async Task<ConsumptionSummary> GetSummaryAsync(Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var daily = await _db.Measurements.AsNoTracking()
            .Where(x => x.UserId == userId && x.MeterId == meterId && x.TimestampUtc >= fromUtc && x.TimestampUtc <= toUtc)
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

    public async Task<(DateTime? MinTimestampUtc, DateTime? MaxTimestampUtc)> GetMeasurementRangeAsync(Guid userId, Guid meterId, CancellationToken ct = default)
    {
        var query = _db.Measurements.AsNoTracking().Where(x => x.UserId == userId && x.MeterId == meterId);

        var min = await query.MinAsync(x => (DateTime?)x.TimestampUtc, ct);
        var max = await query.MaxAsync(x => (DateTime?)x.TimestampUtc, ct);

        return (min, max);
    }
}
