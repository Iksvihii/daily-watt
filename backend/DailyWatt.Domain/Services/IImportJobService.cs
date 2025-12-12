using DailyWatt.Domain.Entities;

namespace DailyWatt.Domain.Services;

public interface IImportJobService
{
    Task<ImportJob> CreateJobAsync(Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task<List<ImportJob>> GetPendingJobsAsync(CancellationToken ct = default);
    Task<ImportJob?> GetAsync(Guid jobId, CancellationToken ct = default);
    Task MarkRunningAsync(ImportJob job, CancellationToken ct = default);
    Task MarkCompletedAsync(ImportJob job, int importedCount, CancellationToken ct = default);
    Task MarkFailedAsync(ImportJob job, string errorCode, string errorMessage, CancellationToken ct = default);
}
