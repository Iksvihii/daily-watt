using System;
using System.Linq;
using ClosedXML.Excel;

class Program
{
  static void Main()
  {
    const string filePath = @"c:\sources\Github\daily-watt\23157163490924_Export_courbe_de_charge_Consommation_01122025-08122025.xlsx";

    try
    {
      using var workbook = new XLWorkbook(filePath);
      var ws = workbook.Worksheets.FirstOrDefault(w => w.Name == "Consommation Horaire");

      if (ws == null)
      {
        Console.WriteLine("✗ Worksheet not found");
        return;
      }

      // Find header row (looking for "Début")
      int? headerRow = null;
      var usedRange = ws.RangeUsed();
      if (usedRange != null)
      {
        for (int row = usedRange.FirstRow().RowNumber(); row <= usedRange.LastRow().RowNumber(); row++)
        {
          for (int col = 1; col <= ws.LastColumnUsed().ColumnNumber(); col++)
          {
            var cell = ws.Cell(row, col);
            if ("Début".Equals(cell.GetString()?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
              headerRow = row;
              break;
            }
          }
          if (headerRow.HasValue) break;
        }
      }

      if (!headerRow.HasValue)
      {
        Console.WriteLine("✗ Header row not found");
        return;
      }

      Console.WriteLine($"✓ Found header row at: {headerRow.Value}");

      // Find column indices
      int debutCol = 0, finCol = 0, valeurCol = 0;
      for (int col = 1; col <= ws.LastColumnUsed().ColumnNumber(); col++)
      {
        var cell = ws.Cell(headerRow.Value, col);
        var header = cell.GetString()?.Trim();
        if ("Début".Equals(header, StringComparison.OrdinalIgnoreCase)) debutCol = col;
        else if ("Fin".Equals(header, StringComparison.OrdinalIgnoreCase)) finCol = col;
        else if ("Valeur (en kW)".Equals(header, StringComparison.OrdinalIgnoreCase)) valeurCol = col;
      }

      Console.WriteLine($"✓ Column indices: Début={debutCol}, Fin={finCol}, Valeur={valeurCol}");

      if (debutCol == 0 || finCol == 0 || valeurCol == 0)
      {
        Console.WriteLine("✗ Required columns not found");
        return;
      }

      // Parse first 10 data rows
      int lastRow = ws.LastRowUsed().RowNumber();
      int dataRows = 0;
      int successfulParsed = 0;

      Console.WriteLine("\n=== Parsing first 10 data rows ===");
      for (int row = headerRow.Value + 1; row <= Math.Min(headerRow.Value + 10, lastRow); row++)
      {
        var debutStr = ws.Cell(row, debutCol).GetString();
        var finStr = ws.Cell(row, finCol).GetString();
        var valeurStr = ws.Cell(row, valeurCol).GetString();

        if (string.IsNullOrWhiteSpace(debutStr) && string.IsNullOrWhiteSpace(finStr))
          continue;

        dataRows++;

        // Try to parse
        var formats = new[] { "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss" };
        bool debutParsed = DateTime.TryParseExact(debutStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var debut);
        bool valeurParsed = double.TryParse(valeurStr?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var kw);

        if (debutParsed && valeurParsed)
        {
          successfulParsed++;
          var kwh = kw * 0.5; // Convert kW to kWh for 30-min interval
          Console.WriteLine($"  Row {row}: {debut:yyyy-MM-dd HH:mm:ss} | {kw:F2} kW → {kwh:F3} kWh ✓");
        }
        else
        {
          Console.WriteLine($"  Row {row}: {debutStr} | {valeurStr} - Parse failed (Début={debutParsed}, Valeur={valeurParsed}) ✗");
        }
      }

      Console.WriteLine($"\n✓ Parsed {successfulParsed}/{dataRows} rows successfully");
      Console.WriteLine($"✓ Total data rows in file: {lastRow - headerRow.Value}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"✗ Error: {ex.Message}");
      Console.WriteLine(ex.StackTrace);
    }
  }
}
