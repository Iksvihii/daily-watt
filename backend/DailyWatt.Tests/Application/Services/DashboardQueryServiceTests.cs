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
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var aggregatedPoints = new List<AggregatedConsumptionPoint>
        {
            new(fromUtc, 10.5),
            new(fromUtc.AddDays(1), 12.3)
        };

        var summary = new ConsumptionSummary(45.0, 15.0, 20.0, DateOnly.FromDateTime(toUtc));

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var mockWeatherService = new Mock<IWeatherProviderService>();
        var mockCredentialService = new Mock<IEnedisCredentialService>();
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
            mockWeatherService.Object,
            mockCredentialService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, fromUtc, toUtc, Granularity.Day, false);

        Assert.NotNull(result);
        Assert.Equal(2, result.Consumption.Count);
        Assert.NotNull(result.Summary);
        Assert.Equal(45.0, result.Summary.TotalKwh);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WithWeatherFalse_DoesNotFetchWeather()
    {
        var userId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var aggregatedPoints = new List<AggregatedConsumptionPoint>();
        var summary = new ConsumptionSummary(0, 0, 0, null);

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Hour, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var mockWeatherService = new Mock<IWeatherProviderService>();
        var mockCredentialService = new Mock<IEnedisCredentialService>();
        var mockMapper = new Mock<IMapper>();

        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>());

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto());

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherService.Object,
            mockCredentialService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, fromUtc, toUtc, Granularity.Hour, false);

        Assert.NotNull(result);
        mockWeatherService.Verify(w => w.GetWeatherAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WithWeatherTrue_FetchesWeatherIfCredentialsExist()
    {
        var userId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var aggregatedPoints = new List<AggregatedConsumptionPoint>();
        var summary = new ConsumptionSummary(0, 0, 0, null);
        var credential = new EnedisCredential { UserId = userId, Latitude = 48.8566, Longitude = 2.3522 };
        var weatherData = new List<WeatherData> { new("2025-12-01", 10.5, 8.2, 12.8) };

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Hour, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var mockWeatherService = new Mock<IWeatherProviderService>();
        mockWeatherService
            .Setup(w => w.GetWeatherAsync(48.8566, 2.3522, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(weatherData);

        var mockCredentialService = new Mock<IEnedisCredentialService>();
        mockCredentialService
            .Setup(c => c.GetCredentialsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var mockMapper = new Mock<IMapper>();
        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>());

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto());

        mockMapper
            .Setup(m => m.Map<List<WeatherDayDto>>(It.IsAny<List<WeatherData>>()))
            .Returns(new List<WeatherDayDto>());

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherService.Object,
            mockCredentialService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, fromUtc, toUtc, Granularity.Hour, true);

        Assert.NotNull(result);
        mockWeatherService.Verify(
            w => w.GetWeatherAsync(48.8566, 2.3522, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTimeSeriesAsync_WithEmptyData_ReturnsEmptyResponse()
    {
        var userId = Guid.NewGuid();
        var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

        var aggregatedPoints = new List<AggregatedConsumptionPoint>();
        var summary = new ConsumptionSummary(0, 0, 0, null);

        var mockConsumptionService = new Mock<IConsumptionService>();
        mockConsumptionService
            .Setup(s => s.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aggregatedPoints);

        mockConsumptionService
            .Setup(s => s.GetSummaryAsync(userId, fromUtc, toUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var mockWeatherService = new Mock<IWeatherProviderService>();
        var mockCredentialService = new Mock<IEnedisCredentialService>();
        var mockMapper = new Mock<IMapper>();

        mockMapper
            .Setup(m => m.Map<List<ConsumptionPointDto>>(It.IsAny<List<AggregatedConsumptionPoint>>()))
            .Returns(new List<ConsumptionPointDto>());

        mockMapper
            .Setup(m => m.Map<SummaryDto>(It.IsAny<ConsumptionSummary>()))
            .Returns(new SummaryDto());

        var service = new DashboardQueryService(
            mockConsumptionService.Object,
            mockWeatherService.Object,
            mockCredentialService.Object,
            mockMapper.Object);

        var result = await service.GetTimeSeriesAsync(userId, fromUtc, toUtc, Granularity.Day, false);

        Assert.NotNull(result);
        Assert.Empty(result.Consumption);
        Assert.NotNull(result.Summary);
    }
}
