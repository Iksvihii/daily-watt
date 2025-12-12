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
    "23157163490924_Export_energie_Consommation_13122022-11122025.xlsx"
  );

  [Fact]
  public void Parse_WithValidDailyExcelFile_ReturnsListOfMeasurements()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var meterId = Guid.NewGuid();

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, meterId);

    // Assert
    Assert.NotEmpty(measurements);
    Assert.All(measurements, m =>
    {
      Assert.Equal(userId, m.UserId);
      Assert.True(m.Kwh >= 0);
      Assert.Equal("enedis", m.Source);
      // Daily file should have midnight timestamps
      Assert.Equal(0, m.TimestampUtc.Hour);
      Assert.Equal(0, m.TimestampUtc.Minute);
      Assert.Equal(0, m.TimestampUtc.Second);
    });
  }

  [Fact]
  public void Parse_ProducesMidnightTimestamps_ForDailyRows()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var meterId = Guid.NewGuid();

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, meterId);

    // Assert
    // Data exists - get first measurement and verify conversion
    Assert.NotEmpty(measurements);
    var firstMeasurement = measurements.FirstOrDefault();
    Assert.NotNull(firstMeasurement);
    Assert.NotNull(firstMeasurement);
    Assert.Equal(0, firstMeasurement!.TimestampUtc.Hour);
    Assert.Equal(0, firstMeasurement.TimestampUtc.Minute);
    Assert.Equal(0, firstMeasurement.TimestampUtc.Second);
  }

  [Fact]
  public void Parse_ExtractsAllDataFromFile()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var meterId = Guid.NewGuid();

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, meterId);

    // Assert - all measurements should be extracted, no filtering
    Assert.NotEmpty(measurements);
    // Verify measurements span a reasonable date range
    var minDate = measurements.Min(m => m.TimestampUtc);
    var maxDate = measurements.Max(m => m.TimestampUtc);
    Assert.True(maxDate > minDate);
  }

  [Fact]
  public void Parse_WithFutureDateFile_ReturnsAllData()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var meterId = Guid.NewGuid();

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, meterId);

    // Assert - should extract all data regardless of date
    Assert.NotEmpty(measurements);
  }

  [Fact]
  public void Parse_EnsuresTimestampIsUtc_ForDaily()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var meterId = Guid.NewGuid();

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, meterId);

    // Assert
    Assert.All(measurements, m =>
    {
      Assert.Equal(DateTimeKind.Utc, m.TimestampUtc.Kind);
    });
  }

  [Fact]
  public void Parse_WithValidDailyFile_ParsesExpectedNumberOfRows()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var meterId = Guid.NewGuid();

    using var stream = File.OpenRead(_testDataPath);

    // Act
    var measurements = ExcelMeasurementParser.Parse(stream, userId, meterId);

    // Assert
    // Compute expected row count by reading the daily worksheet directly
    using var stream2 = File.OpenRead(_testDataPath);
    using var workbook = new ClosedXML.Excel.XLWorkbook(stream2);
    var ws = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Export Consommation Quotidienne");
    Assert.NotNull(ws);

    // Find header row containing 'Date'
    var used = ws!.RangeUsed();
    Assert.NotNull(used);
    int? headerRow = null;
    for (int r = used!.FirstRow().RowNumber(); r <= used.LastRow().RowNumber(); r++)
    {
      var row = ws.Row(r);
      var lastCol = ws.LastColumnUsed();
      if (lastCol == null) continue;
      for (int c = 1; c <= lastCol.ColumnNumber(); c++)
      {
        var cell = row.Cell(c);
        if ((cell.GetString() ?? string.Empty).Trim().Equals("Date", StringComparison.OrdinalIgnoreCase))
        {
          headerRow = r;
          break;
        }
      }
      if (headerRow.HasValue) break;
    }

    Assert.True(headerRow.HasValue, "Header row not found in daily worksheet");

    var firstDataRow = headerRow!.Value + 1;
    var lastRow = ws.LastRowUsed();
    Assert.NotNull(lastRow);
    var expectedRows = 0;
    for (int r = firstDataRow; r <= lastRow!.RowNumber(); r++)
    {
      var dateVal = ws.Row(r).Cell(1).GetString(); // Date is typically first column
      var consVal = ws.Row(r).Cell(2).GetString(); // Valeur (en kWh) typically second
      if (!string.IsNullOrWhiteSpace(dateVal) && !string.IsNullOrWhiteSpace(consVal))
      {
        expectedRows++;
      }
    }

    Assert.Equal(expectedRows, measurements.Count);
  }

  [Fact]
  public void Parse_WithEmptyStream_ThrowsFileFormatException()
  {
    // Arrange
    using var emptyStream = new MemoryStream();
    var userId = Guid.NewGuid();
    var meterId = Guid.NewGuid();

    // Act & Assert
    // ClosedXML throws FileFormatException when stream is empty
    Assert.Throws<System.IO.FileFormatException>(() => ExcelMeasurementParser.Parse(emptyStream, userId, meterId));
  }

  [Fact]
  public void Parse_WithInvalidWorksheet_ThrowsInvalidOperationException()
  {
    // Arrange
    // Create a temporary Excel file with wrong sheet name
    var tempPath = Path.GetTempPath();
    var tempFile = Path.Combine(tempPath, $"test_{Guid.NewGuid()}.xlsx");

    try
    {
      // Create a temporary Excel file with the correct structure but wrong sheet name
      using (var workbook = new ClosedXML.Excel.XLWorkbook())
      {
        var worksheet = workbook.Worksheets.Add("WrongSheetName");
        worksheet.Cell(1, 1).Value = "Test";
        workbook.SaveAs(tempFile);
      }

      using var stream = File.OpenRead(tempFile);
      var userId = Guid.NewGuid();
      var meterId = Guid.NewGuid();

      // Act & Assert
      var ex = Assert.Throws<InvalidOperationException>(() =>
          ExcelMeasurementParser.Parse(stream, userId, meterId));

      Assert.Contains("not found", ex.Message);
    }
    finally
    {
      if (File.Exists(tempFile))
        File.Delete(tempFile);
    }
  }
}
