# DailyWatt Frontend (Angular)

## Project Structure

```
frontend/dailywatt-web/
├── src/
│   ├── app/
│   │   ├── components/      # Standalone components
│   │   ├── services/        # HTTP and business logic services
│   │   ├── models/          # TypeScript interfaces and types
│   │   └── app.routes.ts    # Application routing
│   ├── environments/        # Environment configurations
│   ├── styles.css          # Global styles (LESS preprocessed)
│   ├── index.html          # Root HTML
│   └── main.ts             # Bootstrap with providers
├── angular.json            # Angular CLI configuration
└── tsconfig.json          # TypeScript configuration
```

## Key Technologies

- **Angular 20** - Latest stable version with signals support
- **TypeScript 5.8** - Strict mode enabled
- **LESS** - CSS preprocessing for maintainability
- **Standalone Components** - No NgModules, tree-shakable by default
- **Signals** - New reactive primitive (zone-less architecture)
- **RxJS** - For HTTP operations and async streams

## Architecture

### Zone-less (Signal-based Reactivity)
- `zone.js` is **disabled** in `polyfills.ts`
- All state management uses Angular `signal()`
- Components detect changes via signal subscriptions
- Improved performance and memory usage

### Signals Pattern
State is managed with `signal()` and computed properties:
```typescript
export class MyComponent {
  loading = signal(false);
  data = signal<Data | undefined>(undefined);
  
  load() {
    this.loading.set(true);
    // ...
    this.loading.set(false);
  }
}
```

Templates access signals by calling them:
```html
@if (loading()) {
  <p>Loading...</p>
}
```

### Component Organization
- **Standalone components** - Self-contained with all dependencies declared
- **Service injection** - HTTP services via constructor DI
- **Template syntax** - Modern `@if`, `@for` control flow

### Services
- `AuthService` - Authentication with JWT token storage
- `DashboardService` - Consumption data queries
- `EnedisService` - Enedis API integration
- `AuthInterceptor` - Adds JWT token to requests

## Development

### Installation
```bash
npm install
```

### Dev Server
```bash
npm start
```
Serves on `http://localhost:4200` (or next available port).

### Build
```bash
npm run build
```
Production build in `dist/` directory.

### Testing
```bash
npm test
```

### Code Formatting
- **TypeScript**: Prettier (configured in `.prettierrc`)
- **Styles**: LESS compiler (built-in with Angular CLI)
- Auto-format on save (configured in `.vscode/settings.json`)

## Code Quality

### TypeScript Strictness
- Strict mode enabled
- No implicit any
- Explicit parameter typing required
- Null/undefined checks required

### Naming Conventions
- Components: PascalCase with `.component.ts` suffix
- Services: PascalCase with `.service.ts` suffix
- Templates: `.component.html` co-located with component
- Styles: `.component.less` co-located with component

### Linting
```bash
npm run lint
```

## Building for Production

1. Update environment URLs in `src/environments/environment.prod.ts`
2. Run `npm run build` to create optimized bundle
3. Deploy `dist/` to static hosting (Nginx, Cloudflare Pages, GitHub Pages, etc.)

## Browser Support

- Chrome (latest)
- Edge (latest)
- Firefox (latest)
- Safari (latest)

## Environment Configuration

Create `.env` file in `frontend/dailywatt-web/` for local development:
```
ANGULAR_API_URL=http://localhost:5000
```

Or configure in `src/environments/environment.ts`.
