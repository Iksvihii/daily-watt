namespace DailyWatt.Domain.Models;

public record ConsumptionSummary(double TotalKwh, double AvgKwhPerDay, double MaxDayKwh, DateOnly? MaxDay);
