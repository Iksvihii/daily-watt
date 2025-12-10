# Alignement Frontend/Backend - DailyWatt

## ✅ État de la compilation

- **Frontend (Angular 20)**: ✅ Compile sans erreur
- **Backend (.NET 10)**: ✅ Compile sans erreur
- **Configuration**: ✅ Port API corrigé (5077)

## 📡 Configuration API

### Frontend
- **Fichier**: `frontend/dailywatt-web/src/environments/environment.ts`
- **URL API**: `http://localhost:5077`

### Backend
- **Fichier**: `backend/DailyWatt.Api/Properties/launchSettings.json`
- **Port HTTP**: `5077`
- **Port HTTPS**: `7224`

## 🔄 Correspondance des modèles

### 1. Dashboard / Time Series

#### Frontend (`src/app/models/dashboard.models.ts`)
```typescript
interface TimeSeriesResponse {
  consumption: ConsumptionPoint[];
  weather?: WeatherDay[];
  summary: Summary;
}

interface ConsumptionPoint {
  timestampUtc: string;
  kwh: number;
}

interface Summary {
  totalKwh: number;
  avgKwhPerDay: number;
  maxDayKwh: number;
  maxDay?: string | null;
}

interface WeatherDay {
  date: string;
  tempAvg: number;
  tempMin: number;
  tempMax: number;
  source: string;
}
```

#### Backend (`DailyWatt.Application/DTO/Responses`)
```csharp
class TimeSeriesResponse {
  List<ConsumptionPointDto> Consumption
  List<WeatherDayDto>? Weather
  SummaryDto Summary
}

class ConsumptionPointDto {
  DateTime TimestampUtc
  double Kwh
}

class SummaryDto {
  double TotalKwh
  double AvgKwhPerDay
  double MaxDayKwh
  DateOnly? MaxDay
}

class WeatherDayDto {
  string Date
  double TempAvg
  double TempMin
  double TempMax
  string Source
}
```

**✅ Alignement**: Parfait. Les dates sont converties automatiquement (ISO 8601).

### 2. Authentification

#### Frontend (`src/app/models/auth.models.ts`)
```typescript
interface LoginRequest {
  email: string;
  password: string;
}

interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

interface UserProfile {
  email: string;
  username: string;
}

interface UpdateProfileRequest {
  username: string;
}

interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
```

#### Backend (`DailyWatt.Application/DTO/Requests`)
```csharp
record LoginRequest {
  string Email
  string Password
}

record RegisterRequest {
  string Email
  string Username
  string Password
}

record UpdateProfileRequest {
  string Username
}

record ChangePasswordRequest {
  string CurrentPassword
  string NewPassword
}
```

#### Backend (`DailyWatt.Application/DTO/Responses`)
```csharp
class UserProfileDto {
  string Email
  string Username
}
```

**✅ Alignement**: Parfait.

### 3. Enedis

#### Frontend (`src/app/models/enedis.models.ts`)
```typescript
interface SaveCredentialsRequest {
  login: string;
  password: string;
  meterNumber: string;
  address?: string;
  latitude?: number;
  longitude?: number;
}

interface CreateImportJobRequest {
  fromUtc: string;  // ISO 8601
  toUtc: string;    // ISO 8601
}

interface ImportJob {
  id: string;
  createdAt: string;
  completedAt?: string;
  status: ImportJobStatus;
  errorCode?: string;
  errorMessage?: string;
  importedCount: number;
}

interface EnedisStatus {
  configured: boolean;
  meterNumber?: string;
  updatedAt?: string;
}
```

#### Backend (`DailyWatt.Application/DTO`)
```csharp
record SaveEnedisCredentialsRequest {
  string Login
  string Password
  string MeterNumber
  string? Address
  double? Latitude
  double? Longitude
}

record CreateImportJobRequest {
  DateTime FromUtc
  DateTime ToUtc
}

class ImportJobDto {
  Guid Id
  DateTime CreatedAt
  DateTime? CompletedAt
  string Status
  string? ErrorCode
  string? ErrorMessage
  int ImportedCount
}

class EnedisStatusResponse {
  bool Configured
  string? MeterNumber
  DateTime? UpdatedAt
}
```

**✅ Alignement**: Parfait. Les dates ISO 8601 sont converties automatiquement par ASP.NET Core.

## 🛣️ Routes API

Toutes les routes frontend correspondent aux contrôleurs backend :

| Route Frontend | Contrôleur Backend | Méthode |
|---------------|-------------------|---------|
| `POST /api/auth/login` | `AuthController.Login` | ✅ |
| `POST /api/auth/register` | `AuthController.Register` | ✅ |
| `GET /api/auth/me` | `AuthController.GetProfile` | ✅ |
| `PUT /api/auth/profile` | `AuthController.UpdateProfile` | ✅ |
| `POST /api/auth/change-password` | `AuthController.ChangePassword` | ✅ |
| `GET /api/dashboard/timeseries` | `DashboardController.GetTimeSeries` | ✅ |
| `POST /api/enedis/credentials` | `EnedisController.SaveCredentials` | ✅ |
| `GET /api/enedis/status` | `EnedisController.GetStatus` | ✅ |
| `POST /api/enedis/import` | `EnedisController.CreateImportJob` | ✅ |
| `GET /api/enedis/import/{id}` | `EnedisController.GetJob` | ✅ |
| `GET /api/enedis/geocode/suggestions` | `EnedisController.GetAddressSuggestions` | ✅ |
| `POST /api/enedis/geocode` | `EnedisController.GeocodeAddress` | ✅ |

## 🔐 Authentification

- **Type**: JWT Bearer Token
- **Frontend**: `AuthInterceptor` ajoute automatiquement le header `Authorization`
- **Backend**: `[Authorize]` attribute sur les contrôleurs
- **Token Storage**: `localStorage` (clé: `dailywatt_token`)

## 🗄️ Données de démonstration

Le backend seed automatiquement en mode Development :
- **Email**: `demo@dailywatt.com`
- **Mot de passe**: `Demo123!`
- **Données**: 90 jours de consommation (~13,000 mesures)
- **Pattern**: Variation horaire + saisonnière + aléatoire

## 📝 Notes importantes

1. **Conversion des dates**: Le frontend envoie les dates au format ISO 8601 (`new Date().toISOString()`), ce qui est automatiquement parsé par ASP.NET Core en `DateTime`.

2. **Granularité**: Le frontend utilise un type union TypeScript (`"30min" | "hour" | "day" | "month" | "year"`), tandis que le backend utilise un enum C# avec validation via `GranularityHelper.Parse()`.

3. **CORS**: Le backend utilise une politique CORS permissive en développement (`AddPermissiveCors()`).

4. **Error Handling**: Les erreurs backend sont renvoyées avec un objet `{ error: string }` qui est géré par le frontend.

## ✅ Checklist de vérification

- [x] Frontend compile sans erreur TypeScript
- [x] Backend compile sans erreur C#
- [x] Port API configuré correctement (5077)
- [x] Tous les modèles frontend correspondent aux DTOs backend
- [x] Toutes les routes API sont alignées
- [x] Authentification JWT configurée des deux côtés
- [x] Données de démonstration disponibles
- [x] CORS configuré pour le développement local
