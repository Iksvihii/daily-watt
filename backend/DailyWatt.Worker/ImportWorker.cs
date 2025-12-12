using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyWatt.Worker;

public class ImportWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImportWorker> _logger;

    public ImportWorker(IServiceScopeFactory scopeFactory, ILogger<ImportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobs(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while processing import jobs");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessPendingJobs(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IImportJobService>();
        var meterService = scope.ServiceProvider.GetRequiredService<IEnedisMeterService>();
        var credentialService = scope.ServiceProvider.GetRequiredService<IEnedisCredentialService>();
        var scraper = scope.ServiceProvider.GetRequiredService<IEnedisScraper>();
        var secretProtector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
        var consumptionService = scope.ServiceProvider.GetRequiredService<IConsumptionService>();
        var weatherSyncService = scope.ServiceProvider.GetRequiredService<IWeatherSyncService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var jobs = await jobService.GetPendingJobsAsync(ct);
        foreach (var job in jobs)
        {
            _logger.LogInformation("Processing import job {JobId} for user {UserId} meter {MeterId}", job.Id, job.UserId, job.MeterId);
            try
            {
                // Verify meter belongs to user
                var meter = await meterService.GetAsync(job.UserId, job.MeterId, ct);
                if (meter == null)
                {
                    await jobService.MarkFailedAsync(job, "METER_NOT_FOUND", "Meter not found for user", ct);
                    continue;
                }

                var credentials = await credentialService.GetCredentialsAsync(job.UserId, ct);
                if (credentials == null)
                {
                    await jobService.MarkFailedAsync(job, "NO_CREDENTIALS", "No Enedis credentials found", ct);
                    continue;
                }

                var login = secretProtector.Unprotect(credentials.LoginEncrypted);
                var password = secretProtector.Unprotect(credentials.PasswordEncrypted);

                await jobService.MarkRunningAsync(job, ct);
                await using var excelStream = await scraper.DownloadConsumptionCsvAsync(login, password, job.FromUtc, job.ToUtc, ct);
                var measurements = ExcelMeasurementParser.Parse(excelStream, job.UserId, job.MeterId, job.FromUtc, job.ToUtc);

                await db.Measurements
                    .Where(m => m.UserId == job.UserId && m.MeterId == job.MeterId && m.TimestampUtc >= job.FromUtc && m.TimestampUtc <= job.ToUtc)
                    .ExecuteDeleteAsync(ct);

                await consumptionService.BulkInsertAsync(measurements, ct);

                if (meter.Latitude.HasValue && meter.Longitude.HasValue)
                {
                    var fromDate = DateOnly.FromDateTime(job.FromUtc);
                    var toDate = DateOnly.FromDateTime(job.ToUtc);
                    await weatherSyncService.EnsureWeatherAsync(
                        job.UserId,
                        job.MeterId,
                        meter.Latitude.Value,
                        meter.Longitude.Value,
                        fromDate,
                        toDate,
                        ct);
                }

                await jobService.MarkCompletedAsync(job, measurements.Count, ct);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Job {JobId} cancelled", job.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process import job {JobId}", job.Id);
                await jobService.MarkFailedAsync(job, "UNEXPECTED_ERROR", ex.Message, ct);
            }
        }
    }
}
