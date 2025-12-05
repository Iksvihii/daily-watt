using DailyWatt.Domain.Enums;

namespace DailyWatt.Api.Models.Enedis;

public class ImportJobResponse
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ImportJobStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int ImportedCount { get; set; }
}
