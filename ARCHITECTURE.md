# Architecture Overview

## System Architecture

The Daily Watt application follows a layered architecture with clean separation of concerns:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend (Angular)                        │
│                    TypeScript + RxJS + Angular                   │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                        HTTP/REST (JSON)
                                  │
┌─────────────────────────────────▼───────────────────────────────┐
│                    API Layer (DailyWatt.Api)                     │
│  Controllers │ Services │ Extensions │ Mapping │ DTOs │ Options │
├─────────────────────────────────────────────────────────────────┤
│ • AuthController      → Authentication endpoints                │
│ • DashboardController → Consumption & summary endpoints         │
│ • EnedisController    → Credential & import endpoints           │
├─────────────────────────────────────────────────────────────────┤
│ • AuthService         → Auth business logic                     │
│ • DashboardQueryService → Dashboard data composition            │
│ • AutoMapper          → Entity-to-DTO mapping                   │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                    Dependency Injection
                                  │
┌─────────────────────────────────▼───────────────────────────────┐
│              Infrastructure Layer (DailyWatt.Infrastructure)     │
│         Service Implementations │ Data Access │ Settings         │
├─────────────────────────────────────────────────────────────────┤
│ • ConsumptionService      │ IConsumptionService                │
│ • WeatherService          │ IWeatherService                    │
│ • EnedisCredentialService │ IEnedisCredentialService          │
│ • ImportJobService        │ IImportJobService                 │
│ • SecretProtector         │ ISecretProtector                  │
│ • WeatherParser           │ IWeatherParser                    │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                    Entity Framework Core
                                  │
┌─────────────────────────────────▼───────────────────────────────┐
│                  Domain Layer (DailyWatt.Domain)                 │
│  Service Interfaces │ Entities │ Value Objects │ Enums          │
├─────────────────────────────────────────────────────────────────┤
│ • IConsumptionService                                            │
│ • IWeatherService                                                │
│ • IEnedisCredentialService                                       │
│ • IImportJobService                                              │
│ • IEnedisScraper                                                 │
│ • ISecretProtector                                               │
├─────────────────────────────────────────────────────────────────┤
│ Entities:                                                        │
│ • DailyWattUser      (ASP.NET Identity user)                    │
│ • EnedisCredential   (User's Enedis credentials)                │
│ • Measurement        (Consumption data points)                  │
│ • WeatherDay         (Daily weather data)                       │
│ • ImportJob          (Data import job tracking)                 │
├─────────────────────────────────────────────────────────────────┤
│ Value Objects:                                                   │
│ • AggregatedConsumptionPoint                                    │
│ • ConsumptionSummary                                            │
├─────────────────────────────────────────────────────────────────┤
│ Enums:                                                           │
│ • Granularity       (hour, day, week, month, year)             │
│ • ImportJobStatus   (Pending, Running, Completed, Failed)       │
└─────────────────────────────────┬───────────────────────────────┘
                                  │
                        SQLite Database
                                  │
        ┌───────────────────┬─────────────────┬─────────────┐
        │                   │                 │             │
    ┌───▼───┐        ┌──────▼────┐    ┌──────▼────┐   ┌────▼──┐
    │ Users │        │Measurements│    │Weather    │   │Import │
    │       │        │            │    │Days       │   │Jobs   │
    └───────┘        └────────────┘    └───────────┘   └───────┘
        │                   │                 │             │
        └───────────────────┴─────────────────┴─────────────┘
          Relationships via Foreign Keys
```

## Background Processing

```
┌──────────────────────────────────────────────────────────────────┐
│               Worker Service (DailyWatt.Worker)                   │
│                                                                    │
│ • ImportWorker     → Processes import jobs                        │
│ • CsvMeasurementParser → Parses consumption CSV data             │
└──────────────────────────────────────────────────────────────────┘
```

## Data Flow

### Authentication Flow
```
Client Login/Register
    ↓
AuthController
    ↓
AuthService (Business Logic)
    ↓
UserManager (ASP.NET Identity)
    ↓
ApplicationDbContext
    ↓
SQLite Database
    ↓
Return JWT Token
```

### Consumption Query Flow
```
GET /api/dashboard/timeseries
    ↓
DashboardController (Validate parameters)
    ↓
DashboardQueryService (Orchestrate queries)
    ├─→ IConsumptionService.GetAggregatedAsync()
    ├─→ IConsumptionService.GetSummaryAsync()
    └─→ IWeatherService.GetRangeAsync() (if withWeather=true)
    ↓
ConsumptionService (Infrastructure)
    ↓
ApplicationDbContext (Query Measurements)
    ↓
AutoMapper (Entity → DTO)
    ↓
Return TimeSeriesResponse (JSON)
```

### Enedis Import Flow
```
POST /api/enedis/import
    ↓
EnedisController
    ↓
IImportJobService.CreateJobAsync()
    ↓
Create ImportJob Entity (Status: Pending)
    ↓
Worker Service (Background)
    ↓
IEnedisScraper.ScrapeAsync()
    ↓
CsvMeasurementParser.Parse()
    ↓
IConsumptionService.SaveMeasurementsAsync()
    ↓
Update ImportJob (Status: Completed/Failed)
```

## Database Schema

### Users Table
```
Users
├── Id (GUID, PK)
├── UserName (string)
├── Email (string, unique)
├── PasswordHash (string)
└── Timestamps (Created, Modified)
```

### Measurements Table
```
Measurements
├── Id (GUID, PK)
├── UserId (GUID, FK → Users)
├── TimestampUtc (DateTime)
├── Kwh (double)
├── Granularity (enum: hour, day, week, month, year)
└── Timestamps (Created, Modified)
```

### WeatherDays Table
```
WeatherDays
├── Id (GUID, PK)
├── UserId (GUID, FK → Users)
├── Date (DateOnly)
├── TempAvg (double)
├── TempMin (double)
├── TempMax (double)
├── Source (string)
└── Timestamps (Created, Modified)
```

### EnedisCredentials Table
```
EnedisCredentials
├── Id (GUID, PK)
├── UserId (GUID, FK → Users, unique)
├── EncryptedLogin (string)
├── EncryptedPassword (string)
├── MeterNumber (string)
└── Timestamps (Created, Modified, Updated)
```

### ImportJobs Table
```
ImportJobs
├── Id (GUID, PK)
├── UserId (GUID, FK → Users)
├── Status (enum: Pending, Running, Completed, Failed)
├── FromUtc (DateTime)
├── ToUtc (DateTime)
├── ImportedCount (int)
├── ErrorCode (string, nullable)
├── ErrorMessage (string, nullable)
└── Timestamps (Created, Modified, Completed)
```

## Dependency Injection

The DI container is configured in two places:

### Infrastructure Services (DailyWatt.Infrastructure)
- Database context and Entity Framework
- Domain service implementations
- HTTP client for weather API
- ASP.NET Identity services

### Application Services (DailyWatt.Api)
- AutoMapper profiles
- Application-level services (AuthService, DashboardQueryService)

```csharp
// In Program.cs
builder.Services.AddInfrastructure(configuration);  // Domain service impls
builder.Services.AddJwtAuthentication(configuration); // JWT setup
builder.Services.AddAutoMapping();                    // AutoMapper
builder.Services.AddApplicationServices();            // API services
```

## Authentication & Authorization

### JWT Flow
1. User provides credentials (email + password)
2. AuthService validates against Identity database
3. JwtTokenService creates JWT token with claims
4. Client includes token in Authorization header
5. API validates token on protected endpoints

### Token Claims
- `sub` - User ID
- `email` - User email
- Issued/Expiration timestamps
- Standard OIDC claims

## Security

### Credential Protection
- Enedis credentials encrypted at-rest using DPAPI
- Passwords hashed using ASP.NET Identity (PBKDF2)
- No sensitive data in logs or error messages

### API Security
- JWT bearer token authentication
- HTTPS in production
- CORS configured for frontend
- Authorization checks on user-specific endpoints

## Testing Strategy

Currently testing is done via:
- HTTP test file (`DailyWatt.Api.http`)
- Manual API testing
- Visual Studio integrated testing

Future enhancements:
- Unit tests for services
- Integration tests for controllers
- Database migration tests

## Performance Considerations

### Aggregation
- Measurements pre-aggregated by granularity
- Query performance optimized with indexes on UserId + Granularity + Timestamp

### Caching
- Weather data cached in database
- Automatic weather range pre-fetching

### Background Processing
- Heavy imports run asynchronously via Worker
- User gets immediate job ID, polls for status
- No blocking on consumption API

## Deployment

### Database
```powershell
dotnet ef database update
```

### API
```powershell
dotnet publish -c Release
dotnet DailyWatt.Api.dll
```

### Worker
```powershell
dotnet publish -c Release
dotnet DailyWatt.Worker.dll
```

### Frontend
```powershell
npm run build
# Serve from CDN or static file host
```
