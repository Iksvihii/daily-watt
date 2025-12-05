using System.Globalization;
using System.Text;
using DailyWatt.Domain.Services;
using Microsoft.Extensions.Logging;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Stub implementation that simulates downloading a CSV from Enedis.
/// Replace with a real Playwright or PuppeteerSharp scraper.
/// </summary>
public class StubEnedisScraper : IEnedisScraper
{
    private readonly ILogger<StubEnedisScraper> _logger;

    public StubEnedisScraper(ILogger<StubEnedisScraper> logger)
    {
        _logger = logger;
    }

    public Task<Stream> DownloadConsumptionCsvAsync(string login, string password, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        _logger.LogInformation("Simulating Enedis scraping for user {Login} from {From} to {To}", login, fromUtc, toUtc);

        var sb = new StringBuilder();
        sb.AppendLine("timestamp;kwh");
        var current = fromUtc;
        var random = new Random(1234);
        while (current <= toUtc)
        {
            var value = Math.Round(0.3 + random.NextDouble() * 0.4, 3);
            sb.AppendLine($"{current.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)};{value.ToString(CultureInfo.InvariantCulture)}");
            current = current.AddMinutes(30);
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        Stream stream = new MemoryStream(bytes);
        return Task.FromResult(stream);
    }
}
