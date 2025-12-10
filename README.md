# daily-watt

A comprehensive energy consumption tracking application built with .NET and Angular. The application allows users to monitor their electricity consumption data from Enedis (French electricity distributor) and provides weather correlation analysis.

## Project Structure

### Backend (.NET 10)

#### Architecture (Clean Architecture with 4 Layers)

- **DailyWatt.Domain** - Domain layer with business logic interfaces and entities
  - Entities: `DailyWattUser`, `EnedisCredential`, `ImportJob`, `Measurement`, `WeatherDay`
  - Service interfaces: `IConsumptionService`, `IWeatherService`, `IEnedisCredentialService`, `IImportJobService`, `IEnedisScraper`, `ISecretProtector`
  - Enums: `Granularity`, `ImportJobStatus`
  - Value objects: `AggregatedConsumptionPoint`, `ConsumptionSummary`

- **DailyWatt.Infrastructure** - Data access and external service implementations
  - Entity Framework Core with SQLite
  - Service implementations for consumption, weather, credentials, and import jobs
  - Configuration for dependency injection and settings
  - Data Protection API for sensitive data

- **DailyWatt.Application** - Application business logic layer (NEW)
  - Application services: `AuthService`, `DashboardQueryService`
  - DTOs: `ConsumptionPointDto`, `SummaryDto`, `WeatherDayDto`, `UserProfileDto`, `ImportJobDto`, `AuthResponse`
  - AutoMapper profiles for entity-to-DTO mapping
  - Request/Response models as records

- **DailyWatt.Api** - REST API presentation layer
  - Controllers: `AuthController`, `DashboardController`, `EnedisController`
  - JWT token service: `JwtTokenService`
  - Request validation models with Data Annotations
  - Extensions for configuration (Authentication, CORS)

- **DailyWatt.Worker** - Background job processor
  - Import worker for processing Enedis consumption data
  - CSV measurement parser

### Frontend (Angular)

Located in `frontend/dailywatt-web/` with TypeScript and Angular framework.

## API Endpoints

### Authentication (`/api/auth`)
- `POST /register` - Register new user
- `POST /login` - Authenticate user and get JWT token
- `GET /me` - Get current user profile
- `PUT /profile` - Update user profile
- `POST /change-password` - Change user password

### Dashboard (`/api/dashboard`)
- `GET /timeseries` - Get consumption time series with optional weather data
  - Query parameters: `from`, `to`, `granularity`, `startDate` (optional), `endDate` (optional), `withWeather`

### Enedis (`/api/enedis`)
- `POST /credentials` - Save Enedis account credentials
- `GET /status` - Get Enedis configuration status
- `POST /import` - Create consumption data import job
- `GET /import/{jobId}` - Get import job status

## Features

### Authentication
- User registration and login with password hashing
- JWT token-based authentication
- Secure credential storage using Data Protection API

### Consumption Data
- Aggregated consumption data by granularity (hour, day, week, month, year)
- Consumption summary statistics
- Time series data retrieval with optional date range filtering

### Weather Integration
- Weather data correlation with consumption
- Automatic weather data fetching for date ranges
- Multiple weather data sources support

### Enedis Integration
- Secure credential storage
- Background job processing for data import
- CSV parsing and data aggregation
- Job status tracking

## Getting Started

### Build Backend
```powershell
dotnet build backend/DailyWatt.sln
```

### Run API
```powershell
dotnet run --project backend/DailyWatt.Api
```

### Run Worker
```powershell
dotnet run --project backend/DailyWatt.Worker
```

### Frontend Setup
```powershell
npm install --prefix frontend/dailywatt-web
npm start --prefix frontend/dailywatt-web
```

## Running All Services
Use the compound task from VS Code:
- Task: "run all (API + Worker + Web)"

## Technology Stack

### Backend
- **.NET 10** - Modern C# framework
- **Entity Framework Core** - ORM for data access
- **SQLite** - Lightweight database
- **AutoMapper** - Object mapping
- **JWT** - Authentication tokens
- **Data Protection API** - Credential encryption

### Frontend
- **Angular** - Modern web framework
- **TypeScript** - Typed JavaScript
- **RxJS** - Reactive programming

## Configuration

### Appsettings
- `appsettings.json` - Production configuration
- `appsettings.Development.json` - Development overrides

### JWT Options
Configured in `DailyWatt.Api/Options/JwtOptions.cs`

### Database Connection
Default SQLite connection: `Data Source=dailywatt.db`

## Status

✅ Core architecture established
✅ Authentication system implemented
✅ Dashboard query service created
✅ Dependency injection configured
✅ AutoMapper profiles set up
✅ API HTTP test file available
⏳ Frontend integration in progress
⏳ Enedis scraper implementation pending