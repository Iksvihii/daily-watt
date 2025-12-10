# Guide de test Frontend/Backend

## 🚀 Démarrage de l'application

### 1. Démarrer le backend

```powershell
# Option 1: Via task VS Code
# Utiliser la task: "backend: run API"

# Option 2: Ligne de commande
cd c:\sources\Github\daily-watt
dotnet run --project backend/DailyWatt.Api
```

**Attendu**: 
- L'API démarre sur `http://localhost:5077`
- Les migrations de base de données s'appliquent automatiquement
- Le compte démo est créé (en mode Development)
- Message de log: "Demo user created with email: demo@dailywatt.com"

### 2. Démarrer le frontend

```powershell
# Option 1: Via task VS Code
# Utiliser la task: "frontend: start"

# Option 2: Ligne de commande
cd c:\sources\Github\daily-watt\frontend\dailywatt-web
npm start
```

**Attendu**:
- Le dev server démarre sur `http://localhost:4200`
- L'application Angular se compile sans erreur
- Le navigateur s'ouvre automatiquement

## ✅ Tests fonctionnels

### Test 1: Connexion avec le compte démo

1. Ouvrir `http://localhost:4200`
2. Cliquer sur "Login" (ou aller directement à la page de connexion)
3. Entrer les identifiants :
   - **Email**: `demo@dailywatt.com`
   - **Password**: `Demo123!`
4. Cliquer sur "Sign In"

**Attendu**:
- ✅ Connexion réussie
- ✅ Redirection vers le dashboard
- ✅ Token JWT stocké dans localStorage
- ✅ Pas d'erreur CORS

### Test 2: Affichage des données de consommation

1. Une fois connecté, le dashboard doit afficher:
   - ✅ Un graphique de consommation (90 jours de données)
   - ✅ Les statistiques résumées (Total kWh, moyenne par jour, jour max)
   - ✅ Pas d'erreur de chargement

2. Tester les différentes granularités:
   - ✅ 30 minutes
   - ✅ Heure
   - ✅ Jour (par défaut)
   - ✅ Mois
   - ✅ Année

3. Tester la période de dates:
   - ✅ Modifier les dates de début et fin
   - ✅ Cliquer sur "Load" pour actualiser

**Attendu**:
- Les graphiques se mettent à jour correctement
- Pas d'erreur 404 ou 500
- Les données correspondent aux filtres appliqués

### Test 3: Statut Enedis

1. Dans le dashboard, vérifier la section "Enedis Status"

**Attendu**:
- ✅ Status "Not configured" (le compte démo n'a pas de credentials Enedis)
- ✅ Lien vers "Settings" disponible

### Test 4: Profil utilisateur

1. Aller dans la section "Profile" du dashboard
2. Vérifier les informations:
   - ✅ Email: `demo@dailywatt.com`
   - ✅ Username: `demo@dailywatt.com`

3. Tester la modification du profil:
   - Changer le username (ex: "Demo User")
   - Cliquer sur "Update Profile"

**Attendu**:
- ✅ Mise à jour réussie
- ✅ Message de confirmation

### Test 5: Enregistrement d'un nouvel utilisateur

1. Se déconnecter
2. Aller sur la page "Register"
3. Créer un nouveau compte:
   - Email: `test@example.com`
   - Username: `testuser`
   - Password: `Test123!`

**Attendu**:
- ✅ Compte créé avec succès
- ✅ Connexion automatique après inscription
- ✅ Redirection vers le dashboard (vide, pas de données)

## 🔍 Vérification des appels API

### Avec les DevTools du navigateur

1. Ouvrir les DevTools (F12)
2. Aller dans l'onglet "Network"
3. Se connecter avec le compte démo

**Vérifier**:

#### Requête de login
```
POST http://localhost:5077/api/auth/login
Request: { "email": "demo@dailywatt.com", "password": "Demo123!" }
Response: "<jwt-token>"
Status: 200 OK
```

#### Requête de timeseries
```
GET http://localhost:5077/api/dashboard/timeseries?from=...&to=...&granularity=day&withWeather=true
Headers: Authorization: Bearer <jwt-token>
Response: {
  "consumption": [...],
  "weather": null,
  "summary": { ... }
}
Status: 200 OK
```

#### Requête de statut Enedis
```
GET http://localhost:5077/api/enedis/status
Headers: Authorization: Bearer <jwt-token>
Response: {
  "configured": false,
  "meterNumber": null,
  "updatedAt": null
}
Status: 200 OK
```

## 🐛 Dépannage

### Erreur CORS
**Symptôme**: Erreur dans la console du navigateur concernant CORS

**Solution**:
- Vérifier que le backend est bien en mode Development
- Vérifier que `AddPermissiveCors()` est appelé dans `Program.cs`
- Vérifier que `app.UseCors()` est appelé avant `app.UseAuthentication()`

### Erreur 401 Unauthorized
**Symptôme**: Les requêtes API retournent 401

**Solution**:
- Vérifier que le token JWT est bien stocké dans localStorage
- Vérifier que l'interceptor `AuthInterceptor` est bien configuré
- Vérifier que le header `Authorization: Bearer <token>` est bien envoyé

### Pas de données de consommation
**Symptôme**: Le graphique est vide pour le compte démo

**Solution**:
- Vérifier les logs du backend au démarrage
- Chercher "Demo user created" et "Successfully seeded X measurements"
- Si absent, vérifier que l'environnement est bien "Development"
- Si nécessaire, supprimer la base de données et redémarrer:
  ```powershell
  Remove-Item backend/DailyWatt.Api/dailywatt.db
  dotnet run --project backend/DailyWatt.Api
  ```

### Erreur 404 sur les routes API
**Symptôme**: Les requêtes retournent 404 Not Found

**Solution**:
- Vérifier que l'URL de l'API dans `environment.ts` est bien `http://localhost:5077`
- Vérifier que le backend est bien démarré
- Vérifier les routes dans les contrôleurs backend

## 📊 Vérification des données en base

### Avec SQLite Browser ou depuis le terminal

```powershell
# Installer sqlite3 si nécessaire
# winget install SQLite.SQLite

# Ouvrir la base de données
cd backend/DailyWatt.Api
sqlite3 dailywatt.db

# Vérifier le compte démo
SELECT * FROM AspNetUsers WHERE Email = 'demo@dailywatt.com';

# Compter les mesures
SELECT COUNT(*) FROM Measurements WHERE UserId = '<user-id-from-above>';

# Vérifier les mesures par jour
SELECT DATE(TimestampUtc) as Day, COUNT(*) as Count, SUM(Kwh) as TotalKwh
FROM Measurements 
WHERE UserId = '<user-id>'
GROUP BY Day 
ORDER BY Day DESC 
LIMIT 10;

# Quitter
.quit
```

**Attendu**:
- 1 utilisateur avec email `demo@dailywatt.com`
- ~13,000 mesures (90 jours × 48 mesures/jour)
- Données réparties uniformément sur 90 jours

## ✅ Checklist complète

### Backend
- [ ] Le backend démarre sans erreur
- [ ] Les migrations sont appliquées automatiquement
- [ ] Le compte démo est créé (logs visibles)
- [ ] Les données de démonstration sont seedées (~13,000 mesures)
- [ ] L'API répond sur http://localhost:5077
- [ ] Swagger UI accessible sur http://localhost:5077/swagger

### Frontend
- [ ] Le frontend compile sans erreur TypeScript
- [ ] Le dev server démarre sur http://localhost:4200
- [ ] La page de login s'affiche correctement
- [ ] Connexion avec le compte démo réussie
- [ ] Le dashboard affiche les données de consommation
- [ ] Les graphiques s'affichent correctement
- [ ] Les filtres (dates, granularité) fonctionnent
- [ ] Le profil utilisateur s'affiche
- [ ] L'enregistrement de nouveaux comptes fonctionne

### Intégration
- [ ] Pas d'erreur CORS
- [ ] Les tokens JWT sont correctement gérés
- [ ] Toutes les routes API sont accessibles
- [ ] Les types de données correspondent (dates, nombres, strings)
- [ ] Les erreurs backend sont correctement affichées dans le frontend
