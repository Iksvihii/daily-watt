using Microsoft.AspNetCore.Identity;

namespace DailyWatt.Domain.Entities;

public class DailyWattUser : IdentityUser<Guid>
{
    public ICollection<EnedisCredential> EnedisCredentials { get; set; } = new List<EnedisCredential>();
    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<ImportJob> ImportJobs { get; set; } = new List<ImportJob>();
    public ICollection<WeatherDay> WeatherDays { get; set; } = new List<WeatherDay>();
}
