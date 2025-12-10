using System.Globalization;
using ClosedXML.Excel;
using DailyWatt.Domain.Entities;

namespace DailyWatt.Worker;

/// <summary>
/// Parses Enedis Excel export files (hourly consumption data).
/// Expected structure: Worksheet "Consommation Horaire" with:
/// - Row X: "Plage horaire" marker (header marker row)
/// - Row X+1: Actual column headers ("Début", "Fin", "Valeur (en kW)")
/// - Row X+2+: Data rows with timestamps and consumption values
/// Column positions are typically: C, D, E
/// </summary>
public static class ExcelMeasurementParser
{
  private const string WorksheetName = "Consommation Horaire";
  private const string HeaderPlageHoraire = "Plage horaire";
  private const string HeaderDebut = "Début";
  private const string HeaderFin = "Fin";
  private const string HeaderConsommation = "Valeur (en kW)"; public static List<Measurement> Parse(Stream excelStream, Guid userId, DateTime fromUtc, DateTime toUtc)
  {
    var results = new List<Measurement>();
    excelStream.Seek(0, SeekOrigin.Begin);

    using var workbook = new XLWorkbook(excelStream);

    // Try to find the consumption worksheet
    var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == WorksheetName);
    if (worksheet == null)
    {
      throw new InvalidOperationException($"Worksheet '{WorksheetName}' not found in Excel file");
    }

    // Find header row and column indices
    var headerRow = FindHeaderRow(worksheet);
    if (headerRow == null)
    {
      throw new InvalidOperationException($"Header row with '{HeaderDebut}' not found");
    }

    var debutColIndex = GetColumnIndex(worksheet, headerRow.Value, HeaderDebut);
    var finColIndex = GetColumnIndex(worksheet, headerRow.Value, HeaderFin);
    var consommationColIndex = GetColumnIndex(worksheet, headerRow.Value, HeaderConsommation);

    if (debutColIndex == 0 || finColIndex == 0 || consommationColIndex == 0)
    {
      throw new InvalidOperationException($"Required columns not found. Found: Début={debutColIndex}, Fin={finColIndex}, Valeur={consommationColIndex}");
    }

    // Parse data rows (starting from header row + 1)
    var firstDataRow = headerRow.Value + 1;
    var lastRow = worksheet.LastRowUsed();

    if (lastRow == null)
    {
      return results;
    }

    for (int rowNum = firstDataRow; rowNum <= lastRow.RowNumber(); rowNum++)
    {
      var row = worksheet.Row(rowNum);

      // Get cell values
      var debutCell = row.Cell(debutColIndex);
      var finCell = row.Cell(finColIndex);
      var consommationCell = row.Cell(consommationColIndex);

      // Skip empty rows
      if (debutCell.IsEmpty() && finCell.IsEmpty())
      {
        continue;
      }

      // Parse timestamp from Début column
      if (!TryParseDateTime(debutCell, out var timestamp))
      {
        continue;
      }

      // Parse consumption value (convert kW to kWh for 30-min intervals)
      if (!TryParseConsumption(consommationCell, out var kw))
      {
        continue;
      }

      // Convert kW to kWh (assuming 30-minute intervals: kW * 0.5)
      var kwh = kw * 0.5;      // Ensure timestamp is UTC
      if (timestamp.Kind != DateTimeKind.Utc)
      {
        timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
      }

      // Filter by date range
      if (timestamp < fromUtc || timestamp > toUtc)
      {
        continue;
      }

      results.Add(new Measurement
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        TimestampUtc = timestamp,
        Kwh = kwh,
        Source = "enedis"
      });
    }

    return results;
  }

  /// <summary>
  /// Finds the row number containing the header "Début"
  /// </summary>
  private static int? FindHeaderRow(IXLWorksheet worksheet)
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
        if (HeaderDebut.Equals(cell.GetString()?.Trim(), StringComparison.OrdinalIgnoreCase))
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

  /// <summary>
  /// Attempts to parse a DateTime value from a cell
  /// </summary>
  private static bool TryParseDateTime(IXLCell cell, out DateTime result)
  {
    result = DateTime.MinValue;

    if (cell.IsEmpty())
    {
      return false;
    }

    // Try to get as DateTime if cell is formatted as date
    if (cell.DataType == XLDataType.DateTime)
    {
      try
      {
        result = cell.GetDateTime();
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

    // Try various date formats
    var formats = new[]
    {
            "yyyy-MM-dd HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "yyyy-MM-dd HH:mm"
        };

    if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
    {
      result = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
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
