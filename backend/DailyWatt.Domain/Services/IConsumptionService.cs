using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Models;

namespace DailyWatt.Domain.Services;

public interface IConsumptionService
{
    Task<IReadOnlyList<Measurement>> GetMeasurementsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task<IReadOnlyList<AggregatedConsumptionPoint>> GetAggregatedAsync(Guid userId, DateTime fromUtc, DateTime toUtc, Granularity granularity, CancellationToken ct = default);
    Task<ConsumptionSummary> GetSummaryAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task BulkInsertAsync(IEnumerable<Measurement> measurements, CancellationToken ct = default);
}
