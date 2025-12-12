using AutoMapper;
using DailyWatt.Application.DTO.Responses;
using DailyWatt.Application.Services;
using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Models;
using DailyWatt.Domain.Services;
using Moq;
using Xunit;

namespace DailyWatt.Tests.Application.Services;

public class DashboardQueryServiceTests
{
    [Fact]
    public async Task GetTimeSeriesAsync_ReturnsConsumptionData()
    {
        var userId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var meter = new EnedisMeter
        {
            Id = meterId,
            UserId = userId,
            Prm = "123",
            CreatedAtUtc = DateTime.UtcNow
        };

        var aggregatedPoints = new List<AggregatedConsumptionPoint>
        {
            new(fromUtc, 10.5),
            new(fromUtc.AddDays(1), 12.3)
        };

        var summary = new ConsumptionSummary(45.0, 15.0, 20.0, DateOnly.FromDateTime(toUtc));

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, meterId, fromUtc, toUtc, Granularity.Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, meterId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var mockMeterService = new Mock<IEnedisMeterService>();
        mockMeterService
            .Setup(s => s.GetDefaultMeterAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meter);

        var mockWeatherDataService = new Mock<IWeatherDataService>();
        var mockWeatherSyncService = new Mock<IWeatherSyncService>();
        var mockMapper = new Mock<IMapper>();

        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>
            {
                new() { TimestampUtc = fromUtc, Kwh = 10.5 },
                new() { TimestampUtc = fromUtc.AddDays(1), Kwh = 12.3 }
            });

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto { TotalKwh = 45.0, AvgKwhPerDay = 15.0, MaxDayKwh = 20.0, MaxDay = DateOnly.FromDateTime(toUtc) });

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherSyncService.Object,
            mockWeatherDataService.Object,
            mockMeterService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, null, fromUtc, toUtc, Granularity.Day, false);

        Assert.NotNull(result);
        Assert.Equal(2, result.Consumption.Count);
        Assert.NotNull(result.Summary);
        Assert.Equal(45.0, result.Summary.TotalKwh);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WithWeatherFalse_DoesNotFetchWeather()
    {
        var userId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var meter = new EnedisMeter
        {
            Id = meterId,
            UserId = userId,
            Prm = "123",
            CreatedAtUtc = DateTime.UtcNow
        };

        var aggregatedPoints = new List<AggregatedConsumptionPoint>();
        var summary = new ConsumptionSummary(0, 0, 0, null);

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, meterId, fromUtc, toUtc, Granularity.Hour, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, meterId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var mockWeatherService = new Mock<IWeatherProviderService>();
        var mockWeatherDataService = new Mock<IWeatherDataService>();
        var mockWeatherSyncService = new Mock<IWeatherSyncService>();
        var mockMeterService = new Mock<IEnedisMeterService>();
        mockMeterService
            .Setup(s => s.GetDefaultMeterAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meter);
        var mockMapper = new Mock<IMapper>();

        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>());

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto());

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherSyncService.Object,
            mockWeatherDataService.Object,
            mockMeterService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, null, fromUtc, toUtc, Granularity.Hour, false);

        Assert.NotNull(result);
        mockWeatherSyncService.Verify(s => s.EnsureWeatherAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WithWeatherTrue_FetchesWeatherIfCredentialsExist()
    {
        var userId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var aggregatedPoints = new List<AggregatedConsumptionPoint>();
        var summary = new ConsumptionSummary(0, 0, 0, null);
        var meter = new EnedisMeter { Id = meterId, UserId = userId, Latitude = 48.8566, Longitude = 2.3522, Prm = "123", CreatedAtUtc = DateTime.UtcNow };
        var cachedWeather = new List<WeatherDay>();
        var storedWeather = new List<WeatherDay>
        {
            new()
            {
                UserId = userId,
                Date = DateOnly.FromDateTime(fromUtc),
                TempAvg = 10.5,
                TempMin = 8.2,
                TempMax = 12.8,
                Source = "open-meteo",
                Latitude = 48.8566,
                Longitude = 2.3522,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, meterId, fromUtc, toUtc, Granularity.Hour, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, meterId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        mockConsumptionService
            .Setup(s => s.GetMeasurementRangeAsync(userId, meterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fromUtc, toUtc));

        var mockWeatherSyncService = new Mock<IWeatherSyncService>();
        mockWeatherSyncService
            .Setup(s => s.EnsureWeatherAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockWeatherDataService = new Mock<IWeatherDataService>();
        mockWeatherDataService
            .SetupSequence(w => w.GetAsync(userId, meterId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedWeather)
            .ReturnsAsync(storedWeather);

        var mockMeterService = new Mock<IEnedisMeterService>();
        mockMeterService
            .Setup(s => s.GetDefaultMeterAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meter);

        var mockMapper = new Mock<IMapper>();
        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>());

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto());

        mockMapper
            .Setup(m => m.Map<List<WeatherDayDto>>(It.IsAny<IReadOnlyList<WeatherDay>>()))
            .Returns(new List<WeatherDayDto>());

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherSyncService.Object,
            mockWeatherDataService.Object,
            mockMeterService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, null, fromUtc, toUtc, Granularity.Hour, true);

        Assert.NotNull(result);
        mockWeatherSyncService.Verify(
                s => s.EnsureWeatherAsync(userId, meterId, 48.8566, 2.3522, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WithCachedWeather_SkipsSyncFetch()
    {
        var userId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 3, 23, 59, 59, DateTimeKind.Utc);

        var aggregatedPoints = new List<AggregatedConsumptionPoint>();
        var summary = new ConsumptionSummary(0, 0, 0, null);
        var meter = new EnedisMeter { Id = meterId, UserId = userId, Latitude = 48.8566, Longitude = 2.3522, Prm = "123", CreatedAtUtc = DateTime.UtcNow };

        var cachedWeather = new List<WeatherDay>
    {
      new()
      {
        UserId = userId,
        Date = DateOnly.FromDateTime(fromUtc),
        TempAvg = 12,
        TempMin = 10,
        TempMax = 14,
        Source = "open-meteo",
        Latitude = 48.8566,
        Longitude = 2.3522,
        CreatedAtUtc = DateTime.UtcNow
      }
    };

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, meterId, fromUtc, toUtc, Granularity.Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, meterId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        mockConsumptionService
            .Setup(s => s.GetMeasurementRangeAsync(userId, meterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((fromUtc, toUtc));

        var mockWeatherDataService = new Mock<IWeatherDataService>();
        mockWeatherDataService
            .SetupSequence(w => w.GetAsync(userId, meterId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedWeather)
            .ReturnsAsync(cachedWeather);

        var mockWeatherSyncService = new Mock<IWeatherSyncService>();
        mockWeatherSyncService
            .Setup(s => s.EnsureWeatherAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockMeterService = new Mock<IEnedisMeterService>();
        mockMeterService
            .Setup(s => s.GetDefaultMeterAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meter);

        var mockMapper = new Mock<IMapper>();
        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>());

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto());

        mockMapper
            .Setup(m => m.Map<List<WeatherDayDto>>(It.IsAny<IReadOnlyList<WeatherDay>>()))
            .Returns(new List<WeatherDayDto> { new() { Date = "2025-12-01", TempAvg = 12, TempMin = 10, TempMax = 14, Source = "open-meteo" } });

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherSyncService.Object,
            mockWeatherDataService.Object,
            mockMeterService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, null, fromUtc, toUtc, Granularity.Day, true);

        Assert.NotNull(result);
        Assert.NotNull(result.Weather);
        mockWeatherSyncService.Verify(
                s => s.EnsureWeatherAsync(userId, meterId, 48.8566, 2.3522, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WithEmptyData_ReturnsEmptyResponse()
    {
        var userId = Guid.NewGuid();
        var meterId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var meter = new EnedisMeter { Id = meterId, UserId = userId, Prm = "123", CreatedAtUtc = DateTime.UtcNow };

        var aggregatedPoints = new List<AggregatedConsumptionPoint>();
        var summary = new ConsumptionSummary(0, 0, 0, null);

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, meterId, fromUtc, toUtc, Granularity.Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, meterId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var mockWeatherService = new Mock<IWeatherProviderService>();
        var mockWeatherDataService = new Mock<IWeatherDataService>();
        var mockWeatherSyncService = new Mock<IWeatherSyncService>();
        var mockMeterService = new Mock<IEnedisMeterService>();
        mockMeterService
            .Setup(s => s.GetDefaultMeterAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(meter);
        var mockMapper = new Mock<IMapper>();

        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>());

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto());

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherSyncService.Object,
            mockWeatherDataService.Object,
            mockMeterService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, null, fromUtc, toUtc, Granularity.Day, false);

        Assert.NotNull(result);
        Assert.Empty(result.Consumption);
        Assert.NotNull(result.Summary);
    }
}
