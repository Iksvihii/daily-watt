using AutoMapper;
using DailyWatt.Application.Mapping;
using DailyWatt.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWatt.Application;

/// <summary>
/// Extension methods for registering application layer services.
/// </summary>
public static class DependencyInjection
{
  /// <summary>
  /// Adds application layer services including mapping and business logic.
  /// </summary>
  public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
  {
    // Register AutoMapper
    services.AddAutoMapper(typeof(MappingProfile).Assembly);

    // Register application services
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IDashboardQueryService, DashboardQueryService>();

    return services;
  }
}
