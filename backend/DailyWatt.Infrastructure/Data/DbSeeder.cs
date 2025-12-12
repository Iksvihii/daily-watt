using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DailyWatt.Infrastructure.Data;

public static class DbSeeder
{
  /// <summary>
  /// Seeds the database with demo data for development and testing
  /// </summary>
  public static async Task SeedDemoDataAsync(IServiceProvider serviceProvider)
  {
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DailyWattUser>>();
    var secretProtector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

    try
    {
      // Check if demo user already exists
      var demoUser = await userManager.FindByEmailAsync("demo@dailywatt.com");
      if (demoUser != null)
      {
        logger.LogInformation("Demo user already exists, skipping seed");
        return;
      }

      logger.LogInformation("Creating demo user and consumption data...");

      // Create demo user
      demoUser = new DailyWattUser
      {
        UserName = "demo@dailywatt.com",
        Email = "demo@dailywatt.com",
        EmailConfirmed = true
      };

      var result = await userManager.CreateAsync(demoUser, "Demo123!");
      if (!result.Succeeded)
      {
        logger.LogError("Failed to create demo user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        return;
      }

      logger.LogInformation("Demo user created with email: demo@dailywatt.com and password: Demo123!");

      // Create Enedis credentials for demo user
      var enedisCredential = new EnedisCredential
      {
        UserId = demoUser.Id,
        LoginEncrypted = secretProtector.Protect("demo_login"),
        PasswordEncrypted = secretProtector.Protect("demo_password"),
        UpdatedAt = DateTime.UtcNow
      };

      await context.EnedisCredentials.AddAsync(enedisCredential);

      // Create a demo meter for the user
      var demoMeter = new EnedisMeter
      {
        Id = Guid.NewGuid(),
        UserId = demoUser.Id,
        Prm = "DEMO123456789",
        Label = "Home Meter",
        City = "Paris",
        Latitude = 48.8566,
        Longitude = 2.3522,
        IsFavorite = true,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
      };

      await context.EnedisMeters.AddAsync(demoMeter);

      // Generate realistic daily consumption data
      var measurements = GenerateRealisticDailyConsumptionData(demoUser.Id, demoMeter.Id);

      await context.Measurements.AddRangeAsync(measurements);
      await context.SaveChangesAsync();

      logger.LogInformation("Successfully seeded Enedis credentials, meter, and {Count} measurements for demo user", measurements.Count);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error seeding demo data");
    }
  }

  private static List<Measurement> GenerateRealisticDailyConsumptionData(Guid userId, Guid meterId)
  {
    var measurements = new List<Measurement>();
    var random = new Random(42); // Fixed seed for reproducibility
    var endDate = DateTime.UtcNow.Date;
    var startDate = endDate.AddDays(-90); // 3 months of data

    // Generate one measurement per day (midnight UTC)
    for (var date = startDate; date <= endDate; date = date.AddDays(1))
    {
      var kwh = CalculateRealisticDailyConsumption(date, random);

      measurements.Add(new Measurement
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        MeterId = meterId,
        TimestampUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc),
        Kwh = kwh,
        Source = "demo"
      });
    }

    return measurements;
  }

  private static double CalculateRealisticDailyConsumption(DateTime date, Random random)
  {
    var dayOfWeek = date.DayOfWeek;
    var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;

    // Base daily household consumption in kWh
    double baseDaily = 14.0; // typical baseline

    // Weekend adjustment: people home more
    var weekendMultiplier = isWeekend ? 1.10 : 1.00;

    // Seasonal variation (higher in winter for heating)
    var month = date.Month;
    var seasonalMultiplier = month switch
    {
      12 or 1 or 2 => 1.35,  // Winter
      3 or 11 => 1.15,        // Spring/Fall transition
      4 or 5 or 9 or 10 => 1.00, // Spring/Fall
      6 or 7 or 8 => 0.88,    // Summer (less heating, some AC)
      _ => 1.00
    };

    // Random variation (±15%)
    var randomFactor = 0.85 + (random.NextDouble() * 0.30);

    var dailyKwh = baseDaily * weekendMultiplier * seasonalMultiplier * randomFactor;

    // Round to 3 decimal places
    return Math.Round(dailyKwh, 3);
  }
}
