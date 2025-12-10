namespace DailyWatt.Domain.Entities;

public class EnedisCredential
{
    public Guid UserId { get; set; }
    public byte[] LoginEncrypted { get; set; } = Array.Empty<byte>();
    public byte[] PasswordEncrypted { get; set; } = Array.Empty<byte>();
    public string MeterNumber { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }

    public DailyWattUser? User { get; set; }
}
