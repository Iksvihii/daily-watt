using System.Globalization;
using DailyWatt.Domain.Entities;

namespace DailyWatt.Worker;

public static class CsvMeasurementParser
{
    public static List<Measurement> Parse(Stream csvStream, Guid userId, DateTime fromUtc, DateTime toUtc)
    {
        var results = new List<Measurement>();
        csvStream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(csvStream);
        var firstLine = reader.ReadLine(); // header
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split(';', ',');
            if (parts.Length < 2)
            {
                continue;
            }

            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ts))
            {
                continue;
            }

            if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var kwh))
            {
                continue;
            }

            if (ts < fromUtc || ts > toUtc)
            {
                continue;
            }

            results.Add(new Measurement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TimestampUtc = DateTime.SpecifyKind(ts, DateTimeKind.Utc),
                Kwh = kwh,
                Source = "enedis"
            });
        }

        return results;
    }
}
