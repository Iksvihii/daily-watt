using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Api.Models.Enedis;

public class SaveEnedisCredentialsRequest
{
    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
