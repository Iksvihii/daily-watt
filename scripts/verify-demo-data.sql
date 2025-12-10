-- Script pour vérifier les données de démonstration
-- Exécuter après le démarrage de l'application en mode Development

-- Vérifier l'utilisateur de démo
SELECT 
    Id,
    UserName,
    Email,
    EmailConfirmed
FROM AspNetUsers
WHERE Email = 'demo@dailywatt.com';

-- Statistiques des mesures
SELECT 
    COUNT(*) as TotalMeasurements,
    MIN(TimestampUtc) as FirstMeasurement,
    MAX(TimestampUtc) as LastMeasurement,
    ROUND(AVG(Kwh), 3) as AverageKwh,
    ROUND(SUM(Kwh), 2) as TotalKwh
FROM Measurements
WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email = 'demo@dailywatt.com');

-- Consommation par jour (derniers 7 jours)
SELECT 
    DATE(TimestampUtc) as Date,
    COUNT(*) as MeasurementCount,
    ROUND(SUM(Kwh), 2) as DailyTotal,
    ROUND(AVG(Kwh), 3) as AverageKwh
FROM Measurements
WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email = 'demo@dailywatt.com')
    AND TimestampUtc >= datetime('now', '-7 days')
GROUP BY DATE(TimestampUtc)
ORDER BY Date DESC;

-- Consommation par heure (aujourd'hui)
SELECT 
    strftime('%H:00', TimestampUtc) as Hour,
    COUNT(*) as MeasurementCount,
    ROUND(SUM(Kwh), 2) as HourlyTotal,
    ROUND(AVG(Kwh), 3) as AverageKwh
FROM Measurements
WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email = 'demo@dailywatt.com')
    AND DATE(TimestampUtc) = DATE('now')
GROUP BY strftime('%H', TimestampUtc)
ORDER BY Hour;
