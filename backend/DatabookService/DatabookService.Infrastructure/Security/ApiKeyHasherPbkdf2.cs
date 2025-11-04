using System.Security.Cryptography;
using System.Text;

using DatabookService.Application.Interfaces.Security;

namespace DatabookService.Infrastructure.Security;

public class ApiKeyHasherPbkdf2 : IApiKeyHasher
{
    // format: pbkdf2$iter$salt$b64hash
    private const int Iterations = 100_000;
    private const int SaltSize = 16; // 128-bit
    private const int KeySize = 32; // 256-bit

    public string Hash(string rawKey)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(rawKey),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string rawKey, string hash)
    {
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
        if (!int.TryParse(parts[1], out var iter)) return false;
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(rawKey),
            salt,
            iter,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}


