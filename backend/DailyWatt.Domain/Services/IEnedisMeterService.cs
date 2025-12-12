using DailyWatt.Domain.Entities;

namespace DailyWatt.Domain.Services;

public interface IEnedisMeterService
{
  Task<IReadOnlyList<EnedisMeter>> GetMetersAsync(Guid userId, CancellationToken ct = default);
  Task<EnedisMeter?> GetAsync(Guid userId, Guid meterId, CancellationToken ct = default);
  Task<EnedisMeter> CreateAsync(Guid userId, string prm, string? label, string? city, double? latitude, double? longitude, bool isFavorite, CancellationToken ct = default);
  Task UpdateAsync(Guid userId, Guid meterId, string prm, string? label, string? city, double? latitude, double? longitude, CancellationToken ct = default);
  Task DeleteAsync(Guid userId, Guid meterId, CancellationToken ct = default);
  Task SetFavoriteAsync(Guid userId, Guid meterId, CancellationToken ct = default);
  Task<EnedisMeter?> GetDefaultMeterAsync(Guid userId, CancellationToken ct = default);
}