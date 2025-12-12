using DailyWatt.Domain.Enums;

namespace DailyWatt.Domain.Entities;

public class ImportJob
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MeterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ImportJobStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int ImportedCount { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    public DailyWattUser? User { get; set; }
    public EnedisMeter? Meter { get; set; }
}
