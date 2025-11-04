using System.Buffers.Text;
using System.Security.Cryptography;

using DatabookService.Application.Interfaces.Security;

namespace DatabookService.Infrastructure.Security;

public class ApiKeyGenerator : IApiKeyGenerator
{
    public string Generate(int numBytes = 32, string prefix = "dk_live_")
    {
        var bytes = RandomNumberGenerator.GetBytes(numBytes);
        var base64 = Convert.ToBase64String(bytes);
        // make URL-safe: replace +/ with -_ and trim =
        var urlSafe = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return prefix + urlSafe;
    }
}


