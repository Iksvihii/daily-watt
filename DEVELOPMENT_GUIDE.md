# Development Guide

## Prerequisites

- .NET 10 SDK
- Node.js (v18+)
- npm or yarn
- SQLite (included with .NET)
- Visual Studio Code or Visual Studio

## Project Setup

### Clone and Build

```powershell
# Clone the repository
git clone https://github.com/yourusername/daily-watt.git
cd daily-watt

# Build backend
dotnet build backend/DailyWatt.sln

# Install frontend dependencies
npm install --prefix frontend/dailywatt-web
```

### Database Setup

The application uses SQLite with automatic migrations:

```powershell
# Migrations are applied automatically on startup
# The database file is created at: dailywatt.db

# To manually apply migrations (if needed)
dotnet ef database update --project backend/DailyWatt.Infrastructure
```

## Running the Application

### Option 1: Individual Services

Terminal 1 - API Server:
```powershell
dotnet run --project backend/DailyWatt.Api
# API running at http://localhost:5077
# Swagger at http://localhost:5077/swagger
```

Terminal 2 - Worker Service:
```powershell
dotnet run --project backend/DailyWatt.Worker
```

Terminal 3 - Frontend:
```powershell
npm start --prefix frontend/dailywatt-web
# Frontend running at http://localhost:4200
```

### Option 2: All Services at Once

From VS Code, run the compound task:
- Press `Ctrl+Shift+P` → Tasks: Run Task → "run all (API + Worker + Web)"

## Testing the API

### Using HTTP File (REST Client)

VS Code Extension required: "REST Client" by Huachao Zheng

File location: `backend/DailyWatt.Api/DailyWatt.Api.http`

Steps:
1. Replace `@token` variable with actual JWT token
2. Click "Send Request" above each endpoint
3. View response in side panel

### Using Swagger UI

Navigate to: `http://localhost:5077/swagger`

- Authorize with JWT token
- Try endpoints directly in browser

### Using Curl

```bash
# Register
curl -X POST http://localhost:5077/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "username": "testuser",
    "password": "Password123!"
  }'

# Login
curl -X POST http://localhost:5077/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!"
  }'

# Get time series (replace TOKEN with actual JWT)
curl -X GET "http://localhost:5077/api/dashboard/timeseries?from=2024-01-01&to=2024-01-31&granularity=day&withWeather=true" \
  -H "Authorization: Bearer TOKEN"
```

## Configuration

### appsettings.json Files

#### Backend/DailyWatt.Api/appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "DailyWatt": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=dailywatt.db"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-characters-long-for-production",
    "Issuer": "dailywatt-api",
    "Audience": "dailywatt-client",
    "ExpirationMinutes": 60
  },
  "Weather": {
    "ApiKey": "your-weather-api-key",
    "Provider": "openweathermap" // or "weatherapi"
  }
}
```

#### Frontend/dailywatt-web/environments

Update `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5077/api'
};
```

## Development Workflow

### Adding a New API Endpoint

1. **Create DTO** (if needed)
   ```csharp
   // Models/YourFeature/YourDto.cs
   public class YourDto
   {
       public string Property { get; set; }
   }
   ```

2. **Create/Update Service Interface**
   ```csharp
   // Domain.Services/IYourService.cs
   public interface IYourService
   {
       Task<YourResult> GetDataAsync(Guid userId, CancellationToken ct);
   }
   ```

3. **Implement Service**
   ```csharp
   // Infrastructure/Services/YourService.cs
   public class YourService : IYourService
   {
       // Implementation
   }
   ```

4. **Register Service** (if new)
   ```csharp
   // Infrastructure/DependencyInjection.cs
   services.AddScoped<IYourService, YourService>();
   ```

5. **Create Controller**
   ```csharp
   // Controllers/YourController.cs
   [ApiController]
   [Route("api/[controller]")]
   public class YourController : ControllerBase
   {
       [HttpGet]
       public async Task<ActionResult<YourDto>> GetData()
       {
           // Implementation
       }
   }
   ```

6. **Add AutoMapper Profile** (if DTO mapping needed)
   ```csharp
   // In Mapping/MappingProfile.cs
   CreateMap<YourEntity, YourDto>();
   ```

7. **Add HTTP Test**
   ```
   ### Test your endpoint
   GET http://localhost:5077/api/your
   Authorization: Bearer {{token}}
   ```

### Database Migrations

```powershell
# Create migration
dotnet ef migrations add MigrationName --project backend/DailyWatt.Infrastructure

# Apply migration
dotnet ef database update --project backend/DailyWatt.Infrastructure

# Revert migration
dotnet ef database update PreviousMigrationName --project backend/DailyWatt.Infrastructure

# Remove migration (before applying)
dotnet ef migrations remove --project backend/DailyWatt.Infrastructure
```

### Code Organization

```
DailyWatt.Api/
├── Controllers/       → HTTP endpoints
├── Services/          → Application-level logic (AuthService, DashboardQueryService)
├── Models/            → DTOs and request/response models
├── Extensions/        → Extension methods
├── Helpers/           → Utility functions
├── Mapping/           → AutoMapper profiles
├── Options/           → Configuration options
└── Properties/        → Launch settings, etc.

DailyWatt.Domain/
├── Services/          → Service interfaces
├── Entities/          → Domain models
├── Models/            → Value objects
└── Enums/             → Enumerations

DailyWatt.Infrastructure/
├── Services/          → Service implementations
├── Data/              → Entity Framework context
└── Settings/          → Configuration classes
```

## Common Development Tasks

### Running Tests (when added)
```powershell
dotnet test backend/DailyWatt.sln
```

### Formatting Code
```powershell
dotnet format backend/DailyWatt.sln
```

### Checking for Build Warnings
```powershell
dotnet build backend/DailyWatt.sln --no-incremental /p:TreatWarningsAsErrors=true
```

### Publishing Release Build
```powershell
# API
dotnet publish -c Release -o ./publish/api backend/DailyWatt.Api

# Worker
dotnet publish -c Release -o ./publish/worker backend/DailyWatt.Worker
```

## Troubleshooting

### Database Issues
```powershell
# Remove and recreate database
rm dailywatt.db
dotnet run --project backend/DailyWatt.Api
```

### JWT Token Expired
- Re-login to get a new token
- Update `@token` in HTTP test file

### CORS Errors
- Check `AddPermissiveCors()` in Program.cs
- Ensure frontend URL matches CORS configuration

### Port Already in Use
```powershell
# API default: 5077
# Frontend default: 4200
# Change in launchSettings.json or environment

# Find what's using the port (Windows)
netstat -ano | findstr :5077
taskkill /PID <PID> /F
```

## Debugging

### VS Code Debugger

1. Install "C# Dev Kit" extension
2. Set breakpoints in code
3. Run with debug: `F5`
4. Step through code

### Logging

```csharp
private readonly ILogger<YourClass> _logger;

_logger.LogInformation("Message: {Value}", value);
_logger.LogError(ex, "Error occurred");
```

View logs in terminal output while running.

### Entity Framework Logging

In `appsettings.Development.json`:
```json
"Logging": {
  "LogLevel": {
    "Microsoft.EntityFrameworkCore": "Debug"
  }
}
```

## Performance Tips

1. **Use Select()** to limit fields returned
2. **Use AsNoTracking()** for read-only queries
3. **Eager load related data** with `.Include()`
4. **Create indexes** on frequently queried columns
5. **Cache aggregated results** when possible
6. **Use pagination** for large datasets

## Git Workflow

```powershell
# Create feature branch
git checkout -b feature/your-feature

# Make changes, then commit
git add .
git commit -m "feat: add new endpoint"

# Push to remote
git push origin feature/your-feature

# Create pull request on GitHub
```

## Next Steps

- Implement frontend Angular components
- Add unit tests for services
- Integrate with actual Enedis API
- Set up CI/CD pipeline
- Configure production deployment
- Add API rate limiting
- Implement request logging/monitoring
