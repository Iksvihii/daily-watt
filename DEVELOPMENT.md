# DailyWatt - Development Guide

A full-stack energy consumption tracking application with real-time data visualization and weather integration.

## 🏗️ Architecture Overview

### Technology Stack

**Backend:**
- .NET 10 with ASP.NET Core
- Entity Framework Core
- JWT Authentication
- Swagger/OpenAPI documentation

**Frontend:**
- Angular 20 with Signals
- LESS CSS preprocessing
- Zone-less change detection
- TypeScript 5.8 (strict mode)

**Infrastructure:**
- SQLite (development) / SQL Server (production)
- Background job processing
- Weather API integration (OpenWeatherMap)
- Enedis API integration

## 📁 Directory Structure

```
daily-watt/
├── backend/                    # .NET backend
│   ├── DailyWatt.Api/         # Web API
│   ├── DailyWatt.Domain/      # Domain models
│   ├── DailyWatt.Infrastructure/ # Data & services
│   └── DailyWatt.Worker/      # Background jobs
├── frontend/dailywatt-web/    # Angular SPA
├── .vscode/                   # IDE configuration
│   ├── launch.json           # Debug configurations
│   ├── tasks.json            # Build tasks
│   ├── settings.json         # Workspace settings
│   └── extensions.json       # Recommended extensions
└── README.md                 # This file
```

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- Visual Studio Code or Visual Studio 2024

### First-Time Setup

1. **Clone and enter directory**
   ```bash
   cd daily-watt
   ```

2. **Install frontend dependencies**
   ```bash
   cd frontend/dailywatt-web
   npm install
   cd ../..
   ```

3. **Configure database** (if not using SQLite default)
   - Edit `backend/DailyWatt.Api/appsettings.Development.json`
   - Update connection string as needed

4. **Start all services**
   - Option A: VS Code command palette → "Run Task" → "run all (API + Worker + Web)"
   - Option B: Run in separate terminals:
     ```bash
     # Terminal 1: API
     dotnet run --project backend/DailyWatt.Api
     
     # Terminal 2: Worker
     dotnet run --project backend/DailyWatt.Worker
     
     # Terminal 3: Frontend
     cd frontend/dailywatt-web
     npm start
     ```

5. **Access the application**
   - Frontend: http://localhost:4200
   - API Docs: http://localhost:5000/swagger

## 🔧 Development Workflow

### Backend Development
```bash
# Build solution
dotnet build backend/DailyWatt.sln

# Run with hot reload
dotnet watch --project backend/DailyWatt.Api run

# Create database migration
dotnet ef migrations add MigrationName --project backend/DailyWatt.Infrastructure
dotnet ef database update --project backend/DailyWatt.Infrastructure
```

### Frontend Development
```bash
# Start dev server
npm start

# Run tests
npm test

# Build for production
npm run build

# Format code
npm run format
```

### Debugging

**VS Code:**
1. Set breakpoints in code
2. Press F5 or use Debug menu
3. Select launch configuration:
   - `.NET Launch API (Debugging)` - Backend only
   - `Angular (Chrome)` - Frontend only
   - `🟢 Full Stack (API + Worker + Web)` - Everything

**Visual Studio 2024:**
- Open `backend/DailyWatt.sln`
- Set breakpoints and press F5

## 📚 Architecture Patterns

### Dependency Injection
Services are registered in DI containers using extension methods for clean, modular setup.

### Signals (Frontend)
Modern Angular reactivity primitive replacing RxJS subjects for state management:
```typescript
export class Component {
  count = signal(0);
  increment() { this.count.update(c => c + 1); }
}
```

### Service Layer (Backend)
Business logic separated from controllers via interfaces:
- Services implement domain-specific operations
- Controllers handle HTTP concerns only
- Database queries delegated to EF Core

### DTOs
Data transfer objects prevent coupling API consumers to domain entities.

## 🔐 Security Notes

- JWT tokens are stored in localStorage (frontend)
- All API endpoints except `/auth/*` require `[Authorize]`
- CORS currently allows any origin (configure for production)
- Environment-specific secrets via user-secrets tool

### Production Checklist
- [ ] Set strong JWT secret (minimum 32 characters)
- [ ] Configure CORS to specific origins only
- [ ] Enable HTTPS
- [ ] Set up database backups
- [ ] Configure environment-specific settings
- [ ] Enable request validation and rate limiting

## 📝 Configuration

### JWT Settings (`appsettings.json`)
```json
{
  "Jwt": {
    "Key": "your-secret-key-minimum-32-characters",
    "Issuer": "dailywatt",
    "Audience": "dailywatt-web",
    "ExpirationMinutes": 1440
  }
}
```

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=dailywatt.db"
  }
}
```

### External Services
- Enedis API credentials → stored securely in database
- Weather API → configured in appsettings

## 🧪 Testing

### Frontend Unit Tests
```bash
cd frontend/dailywatt-web
npm test
```

### Backend Unit Tests
```bash
dotnet test backend/DailyWatt.sln
```

## 📦 Deployment

### Docker (Recommended)
```dockerfile
# Build backend
dotnet publish -c Release

# Build frontend
cd frontend/dailywatt-web
npm run build
```

### Cloud Platforms
- **Azure**: Use App Service + SQL Database
- **AWS**: Use EC2 + RDS or ECS + RDS
- **Heroku**: Use buildpacks for .NET and Node.js

## 🤝 Contributing

### Code Style
- .NET: Follow Microsoft C# naming conventions
- TypeScript: Use Prettier formatting
- Commits: Use conventional commit format

### Pull Request Process
1. Create feature branch
2. Make changes and test locally
3. Run full test suite
4. Submit PR with description
5. Address review comments
6. Merge after approval

## 📖 Documentation

Detailed documentation:
- [Backend Architecture](./backend/README.md)
- [Frontend Architecture](./frontend/README.md)
- [API Documentation](http://localhost:5000/swagger) (running)

## ⚠️ Troubleshooting

### Port Already in Use
```bash
# Windows - Find process using port
netstat -ano | findstr :4200
taskkill /PID <PID> /F

# macOS/Linux
lsof -i :4200
kill -9 <PID>
```

### Database Issues
```bash
# Reset database
dotnet ef database drop --project backend/DailyWatt.Infrastructure --force
dotnet ef database update --project backend/DailyWatt.Infrastructure
```

### Module Not Found (Frontend)
```bash
cd frontend/dailywatt-web
rm -rf node_modules package-lock.json
npm install
```

## 🎓 Learning Resources

- [Angular Official Docs](https://angular.io)
- [ASP.NET Core Docs](https://learn.microsoft.com/aspnet/core)
- [Entity Framework Core](https://learn.microsoft.com/ef/core)
- [JWT Authentication](https://jwt.io)

## 📞 Support

For issues or questions:
1. Check existing issues on GitHub
2. Review documentation
3. Create detailed issue report with:
   - Environment (.NET/Node versions)
   - Error message and stack trace
   - Steps to reproduce
   - Expected vs actual behavior

---

**Happy coding! 🚀**
