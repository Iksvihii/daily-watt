using DailyWatt.Domain.Entities;
using DailyWatt.Domain.Services;
using DailyWatt.Infrastructure.Data;
using DailyWatt.Infrastructure.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWatt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection();

        services.Configure<WeatherOptions>(configuration.GetSection(WeatherOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=dailywatt.db";
            options.UseSqlite(connectionString);
        });

        services.AddIdentity<DailyWattUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpClient("weather", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<IGeocodingService, Services.GeocodingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            // Nominatim API requires a User-Agent header with contact information
            client.DefaultRequestHeaders.Add("User-Agent", "DailyWatt/1.0 (Energy Consumption App)");
        });

        services.AddHttpClient<IWeatherProviderService, Services.OpenMeteoWeatherService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            // Open-Meteo requires a User-Agent header
            client.DefaultRequestHeaders.Add("User-Agent", "DailyWatt/1.0 (Energy Consumption App)");
        });

        services.AddScoped<ISecretProtector, Services.SecretProtector>();
        services.AddScoped<IEnedisCredentialService, Services.EnedisCredentialService>();
        services.AddScoped<IEnedisMeterService, Services.EnedisMeterService>();
        services.AddScoped<IConsumptionService, Services.ConsumptionService>();
        services.AddScoped<IImportJobService, Services.ImportJobService>();
        services.AddScoped<IEnedisScraper, Services.StubEnedisScraper>();
        services.AddScoped<IWeatherDataService, Services.WeatherDataService>();
        services.AddScoped<IWeatherSyncService, Services.WeatherSyncService>();

        return services;
    }
}
