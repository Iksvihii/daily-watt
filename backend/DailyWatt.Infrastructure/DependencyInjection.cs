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
        });

        services.AddHttpClient<IWeatherProviderService, Services.OpenMeteoWeatherService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<ISecretProtector, Services.SecretProtector>();
        services.AddScoped<IWeatherParser, Services.WeatherParser>();
        services.AddScoped<IEnedisCredentialService, Services.EnedisCredentialService>();
        services.AddScoped<IImportJobService, Services.ImportJobService>();
        services.AddScoped<IConsumptionService, Services.ConsumptionService>();
        services.AddScoped<IWeatherService, Services.WeatherService>();
        services.AddScoped<IEnedisScraper, Services.StubEnedisScraper>();

        return services;
    }
}
