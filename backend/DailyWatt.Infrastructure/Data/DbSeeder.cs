using DailyWatt.Domain.Entities;
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
        LoginEncrypted = System.Text.Encoding.UTF8.GetBytes("demo_login"),
        PasswordEncrypted = System.Text.Encoding.UTF8.GetBytes("demo_password"),
        MeterNumber = "DEMO123456789",
        City = "Paris",
        Latitude = 48.8566,
        Longitude = 2.3522,
        UpdatedAt = DateTime.UtcNow
      };

      await context.EnedisCredentials.AddAsync(enedisCredential);

      // Generate realistic consumption data
      var measurements = GenerateRealisticConsumptionData(demoUser.Id);

      await context.Measurements.AddRangeAsync(measurements);
      await context.SaveChangesAsync();

      logger.LogInformation("Successfully seeded Enedis credentials and {Count} measurements for demo user", measurements.Count);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error seeding demo data");
    }
  }

  private static List<Measurement> GenerateRealisticConsumptionData(Guid userId)
  {
    var measurements = new List<Measurement>();
    var random = new Random(42); // Fixed seed for reproducibility
    var endDate = DateTime.UtcNow.Date;
    var startDate = endDate.AddDays(-90); // 3 months of data

    // Generate data for each 30-minute interval
    for (var date = startDate; date <= endDate; date = date.AddMinutes(30))
    {
      var kwh = CalculateRealisticConsumption(date, random);

      measurements.Add(new Measurement
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        TimestampUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc),
        Kwh = kwh,
        Source = "demo"
      });
    }

    return measurements;
  }

  private static double CalculateRealisticConsumption(DateTime timestamp, Random random)
  {
    var hour = timestamp.Hour;
    var dayOfWeek = timestamp.DayOfWeek;
    var isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;

    // Base consumption pattern (in kWh for 30 minutes)
    double baseConsumption = hour switch
    {
      // Night (0h-6h): Low consumption (heating, fridge)
      >= 0 and < 6 => 0.15,
      // Morning (6h-9h): Medium consumption (breakfast, getting ready)
      >= 6 and < 9 => isWeekend ? 0.35 : 0.28,
      // Day (9h-17h): Low during week (people at work), medium on weekend
      >= 9 and < 17 => isWeekend ? 0.30 : 0.18,
      // Evening (17h-22h): High consumption (cooking, TV, activities)
      >= 17 and < 22 => 0.45,
      // Late evening (22h-24h): Medium consumption (winding down)
      >= 22 and <= 23 => 0.25,
      _ => 0.20
    };

    // Add seasonal variation (higher in winter for heating)
    var month = timestamp.Month;
    var seasonalMultiplier = month switch
    {
      12 or 1 or 2 => 1.4,  // Winter
      3 or 11 => 1.2,        // Spring/Fall transition
      4 or 5 or 9 or 10 => 1.0, // Spring/Fall
      6 or 7 or 8 => 0.8,    // Summer (less heating, but AC)
      _ => 1.0
    };

    // Add some random variation (±20%)
    var randomFactor = 0.8 + (random.NextDouble() * 0.4);

    // Calculate final consumption
    var consumption = baseConsumption * seasonalMultiplier * randomFactor;

    // Round to 2 decimal places
    return Math.Round(consumption, 3);
  }
}
