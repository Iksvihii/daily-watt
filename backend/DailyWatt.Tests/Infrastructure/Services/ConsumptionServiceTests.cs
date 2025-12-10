using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Enums;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using DailyWatt.Infrastructure.Services;
using DailyWatt.Tests.Infrastructure;
using Moq;
using Xunit;

namespace DailyWatt.Tests.Infrastructure.Services;

public class ConsumptionServiceTests : IClassFixture<TestDatabaseFixture>
{
  private readonly TestDatabaseFixture _fixture;

  public ConsumptionServiceTests(TestDatabaseFixture fixture)
  {
    _fixture = fixture;
  }

  /// <summary>
  /// Creates a test user in the database to satisfy FK constraints
  /// </summary>
  private async Task<Guid> CreateTestUserAsync(ApplicationDbContext dbContext)
  {
    var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var user = new DailyWattUser
    {
      Id = userId,
      UserName = "testuser@example.com",
      Email = "testuser@example.com",
      NormalizedUserName = "TESTUSER@EXAMPLE.COM",
      NormalizedEmail = "TESTUSER@EXAMPLE.COM",
      EmailConfirmed = true,
      SecurityStamp = Guid.NewGuid().ToString()
    };
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();
    return userId;
  }
  [Fact]
  public async Task GetAggregatedAsync_WithHourlyGranularity_AggregatesDataCorrectly()
  {
    // Arrange
    using var dbContext = _fixture.CreateContext();
    var userId = await CreateTestUserAsync(dbContext);

    // Insert test data directly
    dbContext.Measurements.AddRange(
      new Measurement { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), Kwh = 0.31 },
      new Measurement { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = new DateTime(2025, 12, 1, 0, 30, 0, DateTimeKind.Utc), Kwh = 0.22 },
      new Measurement { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = new DateTime(2025, 12, 1, 1, 0, 0, DateTimeKind.Utc), Kwh = 0.28 },
      new Measurement { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = new DateTime(2025, 12, 1, 1, 30, 0, DateTimeKind.Utc), Kwh = 0.25 }
    );
    await dbContext.SaveChangesAsync();

    var consumptionService = new ConsumptionService(dbContext);

    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 1, 23, 59, 59, DateTimeKind.Utc);

    // Act
    var result = await consumptionService.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Hour);

    // Assert
    Assert.NotEmpty(result);
    // Should have 2 hours of data (00:00-01:00 and 01:00-02:00)
    Assert.Equal(2, result.Count);

    var hour0 = result.FirstOrDefault(r => r.TimestampUtc == new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));
    Assert.NotNull(hour0);
    Assert.Equal(0.53, hour0!.Kwh, 2); // 0.31 + 0.22

    var hour1 = result.FirstOrDefault(r => r.TimestampUtc == new DateTime(2025, 12, 1, 1, 0, 0, DateTimeKind.Utc));
    Assert.NotNull(hour1);
    Assert.Equal(0.53, hour1!.Kwh, 2); // 0.28 + 0.25
  }

  [Fact]
  public async Task GetAggregatedAsync_WithDailyGranularity_AggregatesAllDayData()
  {
    // Arrange
    using var dbContext = _fixture.CreateContext();
    var userId = await CreateTestUserAsync(dbContext);

    // Insert 48 measurements for a full day (30-min intervals)
    var measurements = new List<Measurement>();
    for (int i = 0; i < 48; i++)
    {
      measurements.Add(new Measurement
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        TimestampUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i * 30),
        Kwh = 0.5
      });
    }
    dbContext.Measurements.AddRange(measurements);
    await dbContext.SaveChangesAsync();

    var consumptionService = new ConsumptionService(dbContext);

    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 1, 23, 59, 59, DateTimeKind.Utc);

    // Act
    var result = await consumptionService.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Day);

    // Assert
    Assert.Single(result);
    var dayData = result.First();
    Assert.Equal(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), dayData.TimestampUtc);
    Assert.Equal(24.0, dayData.Kwh, 1); // 48 * 0.5 = 24 kWh
  }

  [Fact]
  public async Task GetSummaryAsync_CalculatesSummaryCorrectly()
  {
    // Arrange
    using var dbContext = _fixture.CreateContext();
    var userId = await CreateTestUserAsync(dbContext);

    // Insert test data
    dbContext.Measurements.AddRange(
      new Measurement { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), Kwh = 10 },
      new Measurement { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = new DateTime(2025, 12, 2, 0, 0, 0, DateTimeKind.Utc), Kwh = 15 },
      new Measurement { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = new DateTime(2025, 12, 3, 0, 0, 0, DateTimeKind.Utc), Kwh = 20 }
    );
    await dbContext.SaveChangesAsync();

    var consumptionService = new ConsumptionService(dbContext);

    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 3, 23, 59, 59, DateTimeKind.Utc);

    // Act
    var result = await consumptionService.GetSummaryAsync(userId, fromUtc, toUtc);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(45, result.TotalKwh); // 10 + 15 + 20
    Assert.Equal(15, result.AvgKwhPerDay); // 45 / 3
    Assert.Equal(20, result.MaxDayKwh);
  }

  [Fact]
  public async Task BulkInsertAsync_InsertsMultipleMeasurements()
  {
    // Arrange
    using var dbContext = _fixture.CreateContext();
    var userId = await CreateTestUserAsync(dbContext);
    
    var measurements = new List<Measurement>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = DateTime.UtcNow.AddHours(-2), Kwh = 0.5 },
            new() { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = DateTime.UtcNow.AddHours(-1), Kwh = 0.6 },
            new() { Id = Guid.NewGuid(), UserId = userId, TimestampUtc = DateTime.UtcNow, Kwh = 0.7 },
        };

    var consumptionService = new ConsumptionService(dbContext);

    // Act
    await consumptionService.BulkInsertAsync(measurements);

    // Assert
    // Verify measurements were added to context
    var inserted = dbContext.Measurements.ToList();
    Assert.Equal(measurements.Count, inserted.Count);
  }

  [Fact]
  public async Task GetAggregatedAsync_WithNoData_ReturnsEmptyList()
  {
    // Arrange
    var userId = Guid.NewGuid();
    using var dbContext = _fixture.CreateContext();
    var consumptionService = new ConsumptionService(dbContext);

    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

    // Act
    var result = await consumptionService.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Day);

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public async Task GetAggregatedAsync_WithMonthGranularity_AggregatesMonthlyData()
  {
    // Arrange
    using var dbContext = _fixture.CreateContext();
    var userId = await CreateTestUserAsync(dbContext);

    // Insert test data for 14 days
    var measurements = new List<Measurement>();
    for (int day = 1; day <= 14; day++)
    {
      measurements.Add(new Measurement
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        TimestampUtc = new DateTime(2025, 12, day, 12, 0, 0, DateTimeKind.Utc),
        Kwh = day * 1.0 // Different value per day
      });
    }
    dbContext.Measurements.AddRange(measurements);
    await dbContext.SaveChangesAsync();

    var consumptionService = new ConsumptionService(dbContext);

    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 14, 23, 59, 59, DateTimeKind.Utc);

    // Act
    var result = await consumptionService.GetAggregatedAsync(userId, fromUtc, toUtc, Granularity.Month);

    // Assert
    Assert.Single(result); // All in same month (Dec)
    var monthData = result.First();
    // Sum should be 1+2+3+...+14 = 105
    Assert.Equal(105, monthData.Kwh, 1);
  }
}

/// <summary>
/// Helper class for async enumeration in tests
/// </summary>
public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
  private readonly IEnumerator<T> _inner;

  public TestAsyncEnumerator(IEnumerator<T> inner)
  {
    _inner = inner;
  }

  public T Current => _inner.Current;

  public async ValueTask<bool> MoveNextAsync()
  {
    return await Task.FromResult(_inner.MoveNext());
  }

  public async ValueTask DisposeAsync()
  {
    _inner.Dispose();
    await Task.CompletedTask;
  }
}

/// <summary>
/// Helper class for async query provider in tests
/// </summary>
public class TestAsyncQueryProvider<TEntity> : IQueryProvider
{
  private readonly IQueryProvider _inner;

  public TestAsyncQueryProvider(IQueryProvider inner)
  {
    _inner = inner;
  }

  public IQueryable CreateQuery(Expression expression)
  {
    return new TestAsyncEnumerable<TEntity>(expression);
  }

  public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
  {
    return new TestAsyncEnumerable<TElement>(expression);
  }

  public object Execute(Expression expression)
  {
    return _inner.Execute(expression)!;
  }

  public TResult Execute<TResult>(Expression expression)
  {
    return _inner.Execute<TResult>(expression);
  }

}

/// <summary>
/// Helper class for async enumerable in tests
/// </summary>
public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
  public TestAsyncEnumerable(Expression expression)
      : base(expression)
  {
  }

  public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
  {
    return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
  }

  IQueryProvider IQueryable.Provider
  {
    get { return new TestAsyncQueryProvider<T>(this); }
  }
}
