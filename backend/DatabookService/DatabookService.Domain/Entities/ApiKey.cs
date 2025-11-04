using DatabookService.Domain.Enums;

namespace DatabookService.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string KeyHash { get; private set; }
    public ApiKeyRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ApiKey() { }

    public ApiKey(string name, string keyHash, ApiKeyRole role, DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new ArgumentException("KeyHash cannot be empty", nameof(keyHash));

        Id = Guid.NewGuid();
        Name = name;
        KeyHash = keyHash;
        Role = role;
        IsActive = true;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke()
    {
        IsActive = false;
    }

    public void SetNewHash(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new ArgumentException("Hash cannot be empty", nameof(newHash));
        KeyHash = newHash;
        IsActive = true;
    }

    public bool IsExpired(DateTimeOffset nowUtc)
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= nowUtc;
    }
}


