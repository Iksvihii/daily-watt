using System.Globalization;
using ClosedXML.Excel;
using DailyWatt.Domain.Entities;

namespace DailyWatt.Worker;

/// <summary>
/// Parses Enedis Excel export files.
/// Supported structures:
/// 1) Daily consumption: Worksheet "Export Consommation Quotidienne" with headers:
///    - "Date" (format JJ/MM/AAAA)
///    - "Valeur (en kWh)"
/// 2) Hourly consumption: Worksheet "Consommation Horaire" with headers:
///    - "Début", "Fin", "Valeur (en kW)" (30-min intervals, converted to kWh)
/// The parser will prefer Daily worksheet if present, otherwise fallback to Hourly.
/// </summary>
public static class ExcelMeasurementParser
{
  private const string WorksheetNameDaily = "Export Consommation Quotidienne";
  private const string HeaderDate = "Date";
  private const string HeaderConsommationDaily = "Valeur (en kWh)";

  // No longer supporting hourly worksheet parsing

  public static List<Measurement> Parse(Stream excelStream, Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc)
  {
    excelStream.Seek(0, SeekOrigin.Begin);
    using var workbook = new XLWorkbook(excelStream);

    // Only support daily worksheet
    var dailyWs = workbook.Worksheets.FirstOrDefault(ws => ws.Name == WorksheetNameDaily);
    if (dailyWs == null)
    {
      throw new InvalidOperationException($"Worksheet '{WorksheetNameDaily}' not found in Excel file");
    }

    return ParseDailyWorksheet(dailyWs, userId, meterId, fromUtc, toUtc);
  }

  private static List<Measurement> ParseDailyWorksheet(IXLWorksheet worksheet, Guid userId, Guid meterId, DateTime fromUtc, DateTime toUtc)
  {
    var results = new List<Measurement>();

    var headerRow = FindHeaderRow(worksheet, HeaderDate);
    if (headerRow == null)
    {
      throw new InvalidOperationException($"Header row with '{HeaderDate}' not found in '{WorksheetNameDaily}'");
    }

    var dateColIndex = GetColumnIndex(worksheet, headerRow.Value, HeaderDate);
    var consommationColIndex = GetColumnIndex(worksheet, headerRow.Value, HeaderConsommationDaily);

    if (dateColIndex == 0 || consommationColIndex == 0)
    {
      throw new InvalidOperationException($"Required columns not found. Found: Date={dateColIndex}, Valeur (en kWh)={consommationColIndex}");
    }

    var firstDataRow = headerRow.Value + 1;
    var lastRow = worksheet.LastRowUsed();
    if (lastRow == null)
    {
      return results;
    }

    for (int rowNum = firstDataRow; rowNum <= lastRow.RowNumber(); rowNum++)
    {
      var row = worksheet.Row(rowNum);
      var dateCell = row.Cell(dateColIndex);
      var consommationCell = row.Cell(consommationColIndex);

      if (dateCell.IsEmpty())
      {
        continue;
      }

      if (!TryParseDateOnly(dateCell, out var dateUtc))
      {
        continue;
      }

      if (!TryParseConsumption(consommationCell, out var kwh))
      {
        continue;
      }

      if (dateUtc < fromUtc || dateUtc > toUtc)
      {
        continue;
      }

      results.Add(new Measurement
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        MeterId = meterId,
        TimestampUtc = dateUtc,
        Kwh = kwh,
        Source = "enedis"
      });
    }

    return results;
  }

  // Removed hourly worksheet parser: only daily imports are supported

  /// <summary>
  /// Finds the row number containing the specified header name
  /// </summary>
  private static int? FindHeaderRow(IXLWorksheet worksheet, string headerName)
  {
    var usedRange = worksheet.RangeUsed();
    if (usedRange == null)
    {
      return null;
    }

    for (int row = usedRange.FirstRow().RowNumber(); row <= usedRange.LastRow().RowNumber(); row++)
    {
      var rowObj = worksheet.Row(row);
      var firstCell = rowObj.FirstCell();
      var lastColumn = worksheet.LastColumnUsed();

      if (firstCell == null || lastColumn == null)
      {
        continue;
      }

      for (int col = firstCell.Address.ColumnNumber; col <= lastColumn.ColumnNumber(); col++)
      {
        var cell = worksheet.Cell(row, col);
        if (headerName.Equals(cell.GetString()?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
          return row;
        }
      }
    }

    return null;
  }  /// <summary>
     /// Gets the column index for a given header name in the specified row
     /// </summary>
  private static int GetColumnIndex(IXLWorksheet worksheet, int headerRow, string headerName)
  {
    var row = worksheet.Row(headerRow);
    var lastColumn = worksheet.LastColumnUsed();

    if (lastColumn == null)
    {
      return -1;
    }

    for (int col = 1; col <= lastColumn.ColumnNumber(); col++)
    {
      var cell = row.Cell(col);
      if (headerName.Equals(cell.GetString()?.Trim(), StringComparison.OrdinalIgnoreCase))
      {
        return col;
      }
    }

    return 0; // Not found
  }

  // Removed TryParseDateTime: no hourly parsing anymore

  /// <summary>
  /// Attempts to parse a Date-only value and return DateTime at 00:00 UTC
  /// </summary>
  private static bool TryParseDateOnly(IXLCell cell, out DateTime result)
  {
    result = DateTime.MinValue;

    if (cell.IsEmpty())
    {
      return false;
    }

    if (cell.DataType == XLDataType.DateTime)
    {
      try
      {
        var dt = cell.GetDateTime().Date;
        result = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return true;
      }
      catch { }
    }

    var value = cell.GetString();
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var formats = new[] { "dd/MM/yyyy", "yyyy-MM-dd" };
    if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
    {
      result = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Attempts to parse a consumption (kWh) value from a cell
  /// </summary>
  private static bool TryParseConsumption(IXLCell cell, out double result)
  {
    result = 0;

    if (cell.IsEmpty())
    {
      return false;
    }

    // Try to get as number if cell is numeric
    if (cell.DataType == XLDataType.Number)
    {
      try
      {
        result = cell.GetDouble();
        return true;
      }
      catch
      {
        // Fall through to string parsing
      }
    }

    // Try to parse as string
    var value = cell.GetString();
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    // Replace French decimal separator with invariant separator
    value = value.Replace(",", ".");

    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
    {
      result = parsed;
      return true;
    }

    return false;
  }
}
