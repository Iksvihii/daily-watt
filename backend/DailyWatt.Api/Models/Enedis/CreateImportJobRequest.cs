using System.ComponentModel.DataAnnotations;

namespace DailyWatt.Api.Models.Enedis;

public class CreateImportJobRequest
{
    [Required]
    public DateTime FromUtc { get; set; }

    [Required]
    public DateTime ToUtc { get; set; }
}
