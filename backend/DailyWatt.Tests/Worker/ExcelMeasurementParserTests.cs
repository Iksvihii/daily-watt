using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DailyWatt.Domain.Entities;
using DailyWatt.Worker;
using Xunit;

namespace DailyWatt.Tests.Worker;

public class ExcelMeasurementParserTests
{
  private readonly string _testDataPath = Path.Combine(
      AppContext.BaseDirectory,
      "TestData",
      "23157163490924_Export_courbe_de_charge_Consommation_01122025-08122025.xlsx"
  );

  [Fact]
  public void Parse_WithValidExcelFile_ReturnsListOfMeasurements()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, fromUtc, toUtc);

    // Assert
    Assert.NotEmpty(measurements);
    Assert.All(measurements, m =>
    {
      Assert.Equal(userId, m.UserId);
      Assert.True(m.TimestampUtc >= fromUtc && m.TimestampUtc <= toUtc);
      Assert.True(m.Kwh > 0);
      Assert.Equal("enedis", m.Source);
    });
  }

  [Fact]
  public void Parse_ConvertsKwToKwh_ForThirtyMinuteIntervals()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 1, 2, 0, 0, DateTimeKind.Utc);

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, fromUtc, toUtc);

    // Assert
    // Données Enedis : 01/12 00:00-00:30 = 0.62 kW
    // Conversion attendue : 0.62 * 0.5 = 0.31 kWh
    var firstMeasurement = measurements.FirstOrDefault(m => m.TimestampUtc == new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc));
    Assert.NotNull(firstMeasurement);
    Assert.Equal(0.31, firstMeasurement!.Kwh, 2); // 2 decimal places
  }

  [Fact]
  public void Parse_FiltersDataByDateRange()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var fromUtc = new DateTime(2025, 12, 2, 0, 0, 0, DateTimeKind.Utc); // 2 Dec
    var toUtc = new DateTime(2025, 12, 3, 23, 59, 59, DateTimeKind.Utc);   // 3 Dec

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, fromUtc, toUtc);

    // Assert
    Assert.All(measurements, m =>
    {
      var date = m.TimestampUtc.Date;
      Assert.True(date == new DateTime(2025, 12, 2).Date || date == new DateTime(2025, 12, 3).Date);
    });
  }

  [Fact]
  public void Parse_HandlesNoDataInRange()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Future date
    var toUtc = new DateTime(2026, 1, 2, 23, 59, 59, DateTimeKind.Utc);

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, fromUtc, toUtc);

    // Assert
    Assert.Empty(measurements);
  }

  [Fact]
  public void Parse_EnsuresTimestampIsUtc()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, fromUtc, toUtc);

    // Assert
    Assert.All(measurements, m =>
    {
      Assert.Equal(DateTimeKind.Utc, m.TimestampUtc.Kind);
    });
  }

  [Fact]
  public void Parse_WithValidFile_ParsesExpectedNumberOfRows()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var fromUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var toUtc = new DateTime(2025, 12, 8, 23, 59, 59, DateTimeKind.Utc);

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, fromUtc, toUtc);

    // Assert
    // File has 8 days of data, 48 30-minute intervals per day = 384 intervals expected
    // Verify we get a reasonable number of measurements
    Assert.True(measurements.Count > 330, $"Expected > 330 measurements, got {measurements.Count}");
    Assert.True(measurements.Count < 400, $"Expected < 400 measurements, got {measurements.Count}");
  }

  [Fact]
  public void Parse_WithEmptyStream_ThrowsException()
  {
    // Arrange
    using var emptyStream = new MemoryStream();
    var userId = Guid.NewGuid();
    var fromUtc = DateTime.UtcNow.AddDays(-1);
    var toUtc = DateTime.UtcNow;

    // Act & Assert
    Assert.Throws<Exception>(() => ExcelMeasurementParser.Parse(emptyStream, userId, fromUtc, toUtc));
  }

  [Fact]
  public void Parse_WithInvalidWorksheet_ThrowsOperationCanceledException()
  {
    // Arrange
    // Create an Excel file with a different worksheet name
    var tempFile = Path.GetTempFileName();
    try
    {
      // Create a temporary Excel file with wrong sheet name
      using (var fs = File.Open(tempFile, FileMode.Create))
      {
        var testBytes = File.ReadAllBytes(_testDataPath);
        fs.Write(testBytes, 0, testBytes.Length);
      }

      using var stream = File.OpenRead(tempFile);
      var userId = Guid.NewGuid();
      var fromUtc = DateTime.UtcNow.AddDays(-1);
      var toUtc = DateTime.UtcNow;

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() =>
          ExcelMeasurementParser.Parse(stream, userId, fromUtc, toUtc));

      Assert.Contains("not found", ex.Message);
    }
    finally
    {
      if (File.Exists(tempFile))
        File.Delete(tempFile);
    }
  }
}
