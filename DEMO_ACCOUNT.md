# Compte de Démonstration

Pour faciliter le développement et les tests, un utilisateur de démonstration est automatiquement créé au démarrage de l'application en mode Development.

## Identifiants

- **Email**: `demo@dailywatt.com`
- **Mot de passe**: `Demo123!`

## Données pré-chargées

Le compte de démonstration contient :
- **90 jours** de données de consommation électrique (3 mois)
- Données toutes les **30 minutes** (288 mesures par jour)
- Consommation **réaliste** avec :
  - Variation horaire (faible la nuit, élevée en soirée)
  - Variation saisonnière (plus élevée en hiver pour le chauffage)
  - Différences week-end/semaine
  - Variation aléatoire (±20%) pour plus de réalisme

## Pattern de consommation

Le générateur crée des données avec les patterns suivants :

### Par tranche horaire
- **0h-6h** : ~0.15 kWh/30min (nuit - appareils en veille, chauffage)
- **6h-9h** : ~0.28-0.35 kWh/30min (matin - petit-déjeuner, préparation)
- **9h-17h** : ~0.18-0.30 kWh/30min (journée - varie selon week-end)
- **17h-22h** : ~0.45 kWh/30min (soirée - pic de consommation)
- **22h-24h** : ~0.25 kWh/30min (fin de soirée)

### Par saison
- **Hiver** (déc-fév) : +40% (chauffage)
- **Mi-saison** (mars, nov) : +20%
- **Printemps/Automne** : base
- **Été** (juin-août) : -20% (moins de chauffage, mais climatisation)

## Utilisation

1. Démarrez l'API : `dotnet run --project backend/DailyWatt.Api`
2. Connectez-vous avec les identifiants ci-dessus
3. Les données de consommation sont immédiatement disponibles pour le dashboard

## Régénération

Pour régénérer les données de démo :
1. Supprimez l'utilisateur `demo@dailywatt.com` de la base de données
2. Redémarrez l'application
3. Les données seront automatiquement recréées

## Note

Les données sont générées avec une seed fixe (`Random(42)`) pour garantir la reproductibilité lors des tests et du développement.
