namespace DailyWatt.Domain.Services;

public interface ISecretProtector
{
    byte[] Protect(string plainText);
    string Unprotect(byte[] protectedData);
}
