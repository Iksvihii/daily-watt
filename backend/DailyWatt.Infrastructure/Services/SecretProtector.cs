using System.Text;
using DailyWatt.Domain.Services;
using Microsoft.AspNetCore.DataProtection;

namespace DailyWatt.Infrastructure.Services;

public class SecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public SecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("dailywatt-enedis-credentials");
    }

    public byte[] Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return _protector.Protect(bytes);
    }

    public string Unprotect(byte[] protectedData)
    {
        var bytes = _protector.Unprotect(protectedData);
        return Encoding.UTF8.GetString(bytes);
    }
}
