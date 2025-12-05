namespace DailyWatt.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "DailyWatt";
    public string Audience { get; set; } = "DailyWattUsers";
    public string Key { get; set; } = "please_change_me_very_long_secret";
    public int ExpiresMinutes { get; set; } = 60;
}
