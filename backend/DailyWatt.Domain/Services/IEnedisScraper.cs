namespace DailyWatt.Domain.Services;

public interface IEnedisScraper
{
    Task<Stream> DownloadConsumptionCsvAsync(string login, string password, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
