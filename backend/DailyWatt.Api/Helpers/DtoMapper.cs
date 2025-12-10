using DailyWatt.Api.Models.Enedis;
using DailyWatt.Domain.Entities;

namespace DailyWatt.Api.Helpers;

/// <summary>
/// Maps domain entities to API response DTOs to reduce duplication in controllers.
/// </summary>
public static class DtoMapper
{
  /// <summary>
  /// Maps an ImportJob entity to ImportJobResponse DTO.
  /// </summary>
  public static ImportJobResponse ToResponse(ImportJob job) => new()
  {
    Id = job.Id,
    CreatedAt = job.CreatedAt,
    CompletedAt = job.CompletedAt,
    ErrorCode = job.ErrorCode,
    ErrorMessage = job.ErrorMessage,
    ImportedCount = job.ImportedCount,
    Status = job.Status
  };
}
