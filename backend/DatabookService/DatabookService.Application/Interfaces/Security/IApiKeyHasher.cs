namespace DatabookService.Application.Interfaces.Security;

public interface IApiKeyHasher
{
    string Hash(string rawKey);
    bool Verify(string rawKey, string hash);
}




