using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWatt.Infrastructure.Services;

public class ImportJobService : IImportJobService
{
    private readonly ApplicationDbContext _db;

    public ImportJobService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ImportJob> CreateJobAsync(Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MeterId = meterId,
            CreatedAt = DateTime.UtcNow,
            Status = ImportJobStatus.Pending,
            FromUtc = fromUtc,
            ToUtc = toUtc
        };
        _db.ImportJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<ImportJob> CreateJobWithFileAsync(Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc, string filePath, CancellationToken ct = default)
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MeterId = meterId,
            CreatedAt = DateTime.UtcNow,
            Status = ImportJobStatus.Pending,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            FilePath = filePath
        };
        _db.ImportJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public Task<List<ImportJob>> GetPendingJobsAsync(CancellationToken ct = default)
    {
        return _db.ImportJobs
            .Where(x => x.Status == ImportJobStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<ImportJob?> GetAsync(Guid jobId, CancellationToken ct = default)
    {
        return _db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobId, ct);
    }

    public async Task MarkRunningAsync(ImportJob job, CancellationToken ct = default)
    {
        job.Status = ImportJobStatus.Running;
        _db.ImportJobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkCompletedAsync(ImportJob job, int importedCount, CancellationToken ct = default)
    {
        job.Status = ImportJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        job.ImportedCount = importedCount;
        _db.ImportJobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(ImportJob job, string errorCode, string errorMessage, CancellationToken ct = default)
    {
        job.Status = ImportJobStatus.Failed;
        job.CompletedAt = DateTime.UtcNow;
        job.ErrorCode = errorCode;
        job.ErrorMessage = errorMessage;
        _db.ImportJobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }
}
