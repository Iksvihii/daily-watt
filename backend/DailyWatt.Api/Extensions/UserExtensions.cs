using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyWatt.Api.Extensions;

public static class UserExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return id != null ? Guid.Parse(id) : Guid.Empty;
    }
}
