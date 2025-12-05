using DailyWatt.Domain.Services;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace DailyWatt.Infrastructure.Services;

/// <summary>
/// Puppeteer-based scraper that navigates through the Enedis portal to download the consumption CSV.
/// NOTE: Selectors and URLs are examples; replace with real ones after inspecting the live portal.
/// </summary>
public class StubEnedisScraper : IEnedisScraper
{
    private readonly ILogger<StubEnedisScraper> _logger;

    public StubEnedisScraper(ILogger<StubEnedisScraper> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> DownloadConsumptionCsvAsync(string login, string password, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        // Example placeholders: update URLs and selectors after analyzing the real site.
        const string loginUrl = "https://mon-compte.enedis.fr/auth/login";
        const string consumptionUrl = "https://mon-compte.enedis.fr/mes-donnees/consommation";
        const string loginSelector = "#username";
        const string passwordSelector = "#password";
        const string submitSelector = "button[type=submit]";
        const string dateFromSelector = "#date-from";
        const string dateToSelector = "#date-to";
        const string showButtonSelector = "#btn-show";
        const string downloadButtonSelector = "#btn-download";

        await new BrowserFetcher().DownloadAsync();

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--no-sandbox" }
        });

        await using var page = await browser.NewPageAsync();
        var downloadDir = Path.Combine(Path.GetTempPath(), $"dailywatt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(downloadDir);

        // Allow downloads to temp folder
        var client = await page.Target.CreateCDPSessionAsync();
        await client.SendAsync("Page.setDownloadBehavior", new
        {
            behavior = "allow",
            downloadPath = downloadDir
        });

        _logger.LogInformation("Navigating to login page {Url}", loginUrl);
        await page.GoToAsync(loginUrl, WaitUntilNavigation.DOMContentLoaded);
        await page.WaitForSelectorAsync(loginSelector, new() { Timeout = 30000 }).WaitAsync(ct);
        await page.TypeAsync(loginSelector, login, new() { Delay = 30 });
        await page.TypeAsync(passwordSelector, password, new() { Delay = 30 });
        await Task.WhenAll(
            page.ClickAsync(submitSelector),
            page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } })
        );

        _logger.LogInformation("Navigating to consumption page {Url}", consumptionUrl);
        await page.GoToAsync(consumptionUrl, WaitUntilNavigation.Networkidle0);

        // Fill date range
        await page.WaitForSelectorAsync(dateFromSelector, new() { Timeout = 30000 }).WaitAsync(ct);
        await page.EvaluateFunctionAsync(@"(selector, value) => {
            const el = document.querySelector(selector);
            if (el) { el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); }
        }", dateFromSelector, fromUtc.ToString("yyyy-MM-dd"));
        await page.EvaluateFunctionAsync(@"(selector, value) => {
            const el = document.querySelector(selector);
            if (el) { el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); }
        }", dateToSelector, toUtc.ToString("yyyy-MM-dd"));

        // Show data then download
        await page.ClickAsync(showButtonSelector);
        await page.WaitForNetworkIdleAsync(new() { IdleTime = 1000, Timeout = 15000 });
        await page.ClickAsync(downloadButtonSelector);

        _logger.LogInformation("Waiting for CSV download into {Dir}", downloadDir);
        var csvPath = await WaitForDownloadAsync(downloadDir, TimeSpan.FromSeconds(30), ct);
        if (csvPath == null)
        {
            throw new InvalidOperationException("Download did not complete in time.");
        }

        var bytes = await File.ReadAllBytesAsync(csvPath, ct);
        return new MemoryStream(bytes);
    }

    private static async Task<string?> WaitForDownloadAsync(string folder, TimeSpan timeout, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            ct.ThrowIfCancellationRequested();
            var file = Directory.GetFiles(folder).FirstOrDefault(f => !f.EndsWith(".crdownload", StringComparison.OrdinalIgnoreCase));
            if (file != null)
            {
                return file;
            }
            await Task.Delay(500, ct);
        }

        return null;
    }
}
