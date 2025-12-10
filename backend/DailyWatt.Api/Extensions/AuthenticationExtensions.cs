using System.Text;
using DailyWatt.Api.Options;
using DailyWatt.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace DailyWatt.Api.Extensions;

/// <summary>
/// Extension methods for configuring authentication and authorization services.
/// </summary>
public static class AuthenticationExtensions
{
  /// <summary>
  /// Adds JWT authentication with the configured options.
  /// </summary>
  public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
    services.AddScoped<IJwtTokenService, JwtTokenService>();

    var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

    services.AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
      options.RequireHttpsMetadata = false;
      options.SaveToken = true;
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromMinutes(1)
      };
    });

    services.AddAuthorization();
    return services;
  }

  /// <summary>
  /// Adds CORS policy allowing any origin, header, and method.
  /// Use with caution in production - configure specific origins instead.
  /// </summary>
  public static IServiceCollection AddPermissiveCors(this IServiceCollection services)
  {
    services.AddCors(options =>
    {
      options.AddDefaultPolicy(policy =>
          {
          policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
        });
    });
    return services;
  }
}
