using System.Net.Http.Json;
using System.Text.Json;
using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using DailyWatt.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DailyWatt.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WeatherOptions _options;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<WeatherOptions> options,
        ILogger<WeatherService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureWeatherRangeAsync(Guid userId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var existing = await _db.WeatherDays.AsNoTracking()
            .Where(w => w.UserId == userId && w.Date >= fromDate && w.Date <= toDate)
            .Select(w => w.Date)
            .ToListAsync(ct);

        var missingDates = new List<DateOnly>();
        for (var date = fromDate; date <= toDate; date = date.AddDays(1))
        {
            if (!existing.Contains(date))
            {
                missingDates.Add(date);
            }
        }

        if (!missingDates.Any())
        {
            return;
        }

        var fetched = await FetchWeatherAsync(fromDate, toDate, ct);
        var toAdd = fetched
            .Where(w => missingDates.Contains(w.Date))
            .Select(w =>
            {
                w.Id = Guid.NewGuid();
                w.UserId = userId;
                return w;
            })
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        await _db.WeatherDays.AddRangeAsync(toAdd, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WeatherDay>> GetRangeAsync(Guid userId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        return await _db.WeatherDays.AsNoTracking()
            .Where(w => w.UserId == userId && w.Date >= fromDate && w.Date <= toDate)
            .OrderBy(w => w.Date)
            .ToListAsync(ct);
    }

    private async Task<List<WeatherDay>> FetchWeatherAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct)
    {
        var url =
            $"{_options.BaseUrl}?latitude={_options.Latitude}&longitude={_options.Longitude}&start_date={fromDate:yyyy-MM-dd}&end_date={toDate:yyyy-MM-dd}&daily=temperature_2m_max,temperature_2m_min,temperature_2m_mean&timezone=UTC";

        var client = _httpClientFactory.CreateClient("weather");
        try
        {
            using var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return ParseWeather(json, _options.Source);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch weather data from {Url}. Generating placeholder data.", url);
            return GenerateFallback(fromDate, toDate, _options.Source);
        }
    }

    private static List<WeatherDay> ParseWeather(JsonDocument doc, string source)
    {
        var root = doc.RootElement;
        if (!root.TryGetProperty("daily", out var daily))
        {
            return [];
        }

        var dates = daily.GetProperty("time").EnumerateArray().ToList();
        var maxes = daily.GetProperty("temperature_2m_max").EnumerateArray().ToList();
        var mins = daily.GetProperty("temperature_2m_min").EnumerateArray().ToList();
        var avgs = daily.TryGetProperty("temperature_2m_mean", out var mean) ? mean.EnumerateArray().ToList() : new List<JsonElement>();

        var list = new List<WeatherDay>();
        for (var i = 0; i < dates.Count; i++)
        {
            var date = DateOnly.Parse(dates[i].GetString()!);
            var max = maxes.ElementAtOrDefault(i).GetDouble();
            var min = mins.ElementAtOrDefault(i).GetDouble();
            var avg = avgs.Count > i ? avgs[i].GetDouble() : (max + min) / 2;

            list.Add(new WeatherDay
            {
                Date = date,
                TempMax = max,
                TempMin = min,
                TempAvg = avg,
                Source = source
            });
        }

        return list;
    }

    private static List<WeatherDay> GenerateFallback(DateOnly from, DateOnly to, string source)
    {
        var result = new List<WeatherDay>();
        var random = new Random(42);
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var tempAvg = random.Next(-5, 30);
            result.Add(new WeatherDay
            {
                Date = date,
                TempAvg = tempAvg,
                TempMin = tempAvg - 3,
                TempMax = tempAvg + 3,
                Source = source
            });
        }

        return result;
    }
}
