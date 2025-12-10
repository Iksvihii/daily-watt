# DailyWatt Backend Architecture

## Project Structure

```
backend/
├── DailyWatt.Api/           # ASP.NET Core Web API
├── DailyWatt.Domain/        # Domain models and interfaces
├── DailyWatt.Infrastructure/ # Data access and external services
└── DailyWatt.Worker/        # Background job processor
```

## Key Design Patterns

### Dependency Injection
All services are configured in DI containers using extension methods:
- `AddInfrastructure()` - Infrastructure services (data, external APIs)
- `AddJwtAuthentication()` - JWT authentication and authorization
- `AddPermissiveCors()` - CORS policy configuration

### DTO Mapping
The `DtoMapper` utility class centralizes entity-to-DTO conversions to reduce duplication in controllers.

### Service Layer
Business logic is organized into service interfaces and implementations:
- `IConsumptionService` - Consumption data queries and aggregation
- `IWeatherProviderService` - Real-time weather data from Open-Meteo API
- `IGeocodingService` - Address geocoding via Nominatim API
- `IEnedisCredentialService` - Secure credential storage
- `IImportJobService` - Import job lifecycle
- `IJwtTokenService` - JWT token generation and validation

### API Controllers
RESTful endpoints are organized by feature:
- `AuthController` - Authentication (login, register)
- `DashboardController` - Consumption data and analytics
- `EnedisController` - Enedis integration and import jobs

## Configuration

### JWT Settings
Configured via `appsettings.json`:
```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "dailywatt",
    "Audience": "dailywatt-web"
  }
}
```

### Database
Uses Entity Framework Core with migrations. Database context is in `DailyWatt.Infrastructure.Data.ApplicationDbContext`.

## Development

### Building
```bash
dotnet build backend/DailyWatt.sln
```

### Running
```bash
# API
dotnet run --project backend/DailyWatt.Api

# Worker
dotnet run --project backend/DailyWatt.Worker

# Watch mode (with hot reload)
dotnet watch --project backend/DailyWatt.Api run
```

### Database Migrations
```bash
# Create migration
dotnet ef migrations add MigrationName --project backend/DailyWatt.Infrastructure

# Update database
dotnet ef database update --project backend/DailyWatt.Infrastructure
```

## Code Quality

### Best Practices
- Use async/await for I/O operations
- Use `CancellationToken` throughout for graceful shutdown
- Validate inputs with `[Required]`, `[EmailAddress]`, etc. attributes
- Use meaningful exception messages
- Document public APIs with XML comments

### Authorization
All protected endpoints use `[Authorize]` attribute. The JWT token is extracted from the `Authorization: Bearer {token}` header.

## Security Considerations

⚠️ **Current State**: CORS is set to allow any origin. In production:
- Configure specific allowed origins
- Use HTTPS only
- Set secure JWT secret (minimum 32 characters)
- Implement rate limiting
- Add request validation middleware
