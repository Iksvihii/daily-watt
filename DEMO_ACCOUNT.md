# Compte de Démonstration

Pour faciliter le développement et les tests, un utilisateur de démonstration est automatiquement créé au démarrage de l'application en mode Development.

## Identifiants

- **Email**: `demo@dailywatt.com`
- **Mot de passe**: `Demo123!`

## Données pré-chargées

Le compte de démonstration contient :
- **90 jours** de données de consommation électrique (3 mois)
- Données **quotidiennes** (1 mesure par jour à minuit UTC)
- Consommation **réaliste** avec :
  - Variation saisonnière (plus élevée en hiver pour le chauffage)
  - Différences week-end/semaine
  - Variation aléatoire (±20%) pour plus de réalisme

## Pattern de consommation

Le générateur crée des données avec les patterns suivants :

### Consommation journalière de base
- **Jour de semaine** : ~14 kWh/jour
- **Week-end** : ~16 kWh/jour (+15% pour plus de présence à domicile)

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
