# Documentation API - Scalar

## ✅ Solution adoptée : Scalar.AspNetCore

Pour .NET 10, nous avons remplacé **Swashbuckle (Swagger UI)** par **Scalar**, qui est la solution moderne recommandée pour .NET 10.

### Pourquoi Scalar au lieu de Swagger ?

1. **Compatibilité native** : Scalar est conçu pour .NET 10 et utilise `Microsoft.AspNetCore.OpenApi`
2. **Interface moderne** : Interface utilisateur plus élégante et rapide
3. **Performance** : Meilleure performance que Swagger UI
4. **Fonctionnalités** : Support complet d'OpenAPI 3.1, thèmes, recherche avancée

### URLs disponibles

| Endpoint | Description | URL |
|----------|-------------|-----|
| **Scalar UI** | Interface interactive de documentation API | `http://localhost:5077/scalar/v1` |
| **OpenAPI JSON** | Spécification OpenAPI au format JSON | `http://localhost:5077/openapi/v1.json` |

### Accès à la documentation

1. **Démarrer l'API** :
   ```powershell
   dotnet run --project backend/DailyWatt.Api
   ```

2. **Ouvrir Scalar dans le navigateur** :
   ```
   http://localhost:5077/scalar/v1
   ```

### Fonctionnalités de Scalar

#### 1. Explorateur d'API interactif
- Liste complète des endpoints par contrôleur
- Détails de chaque endpoint (méthode HTTP, route, paramètres)
- Schémas de requête/réponse
- Exemples de code (cURL, JavaScript, Python, etc.)

#### 2. Test des endpoints
- Interface "Try it out" pour tester directement les APIs
- Support de l'authentification JWT (Bearer Token)
- Historique des requêtes

#### 3. Documentation automatique
- Génération automatique à partir des contrôleurs
- Support des attributs `[Authorize]`, `[FromQuery]`, `[FromBody]`, etc.
- Documentation des modèles DTOs

### Configuration JWT dans Scalar

Pour tester les endpoints protégés :

1. Aller dans l'onglet "Authentication" de Scalar
2. Sélectionner "Bearer Token"
3. Se connecter via `/api/auth/login` pour obtenir un token
4. Copier le token JWT reçu
5. Coller le token dans le champ "Token"
6. Tous les appels suivants incluront automatiquement le header `Authorization: Bearer <token>`

### Personnalisation (optionnelle)

Pour personnaliser l'apparence de Scalar, modifier dans `Program.cs` :

```csharp
app.MapScalarApiReference(options =>
{
    options.Title = "DailyWatt API";
    options.Theme = ScalarTheme.Purple;
    options.ShowSidebar = true;
});
```

### Migration depuis Swagger

#### Avant (Swashbuckle)
```csharp
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
```

#### Après (Scalar)
```csharp
builder.Services.AddOpenApi();
app.MapOpenApi();
app.MapScalarApiReference();
```

### Packages NuGet

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="1.2.68" />
```

### Avantages supplémentaires

- ✅ **Pas de problème de compatibilité** avec .NET 10
- ✅ **Plus rapide** que Swagger UI (chargement quasi instantané)
- ✅ **Interface responsive** optimisée pour mobile
- ✅ **Thèmes sombres/clairs** intégrés
- ✅ **Recherche globale** dans tous les endpoints
- ✅ **Export OpenAPI** au format JSON/YAML
- ✅ **Génération de code client** dans plusieurs langages

### Documentation officielle

- Scalar : https://scalar.com/
- GitHub : https://github.com/scalar/scalar
- OpenAPI .NET : https://learn.microsoft.com/aspnet/core/fundamentals/openapi

---

**Note** : L'ancien package Swashbuckle a été retiré du projet car il n'est pas compatible avec .NET 10 (erreur `TypeLoadException: Method 'GetSwagger' does not have an implementation`).
