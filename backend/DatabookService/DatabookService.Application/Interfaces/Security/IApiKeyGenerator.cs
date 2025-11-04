namespace DatabookService.Application.Interfaces.Security;

public interface IApiKeyGenerator
{
    string Generate(int numBytes = 32, string prefix = "dk_live_");
}


