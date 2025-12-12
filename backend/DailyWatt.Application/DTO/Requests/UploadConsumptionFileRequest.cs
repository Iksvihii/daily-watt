using Microsoft.AspNetCore.Http;

namespace DailyWatt.Application.DTO.Requests;

public class UploadConsumptionFileRequest
{
  public IFormFile File { get; set; } = null!;
  public Guid MeterId { get; set; }
  public DateTime FromUtc { get; set; }
  public DateTime ToUtc { get; set; }
}
