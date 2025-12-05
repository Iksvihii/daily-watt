using DailyWatt.Domain.Entities;

namespace DailyWatt.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(DailyWattUser user);
}
