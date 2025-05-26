namespace Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    // Navigation property
    public User User { get; private set; }

    // Constructor privado para EF Core
    private RefreshToken() { }

    // Factory method
    public static RefreshToken Create(
        Guid userId,
        string token,
        DateTimeOffset expiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId no puede estar vacío", nameof(userId));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("El token no puede estar vacío", nameof(token));

        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("La fecha de expiración debe ser futura", nameof(expiresAt));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    // Métodos de negocio
    public void Revoke(string? reason = null)
    {
        if (IsRevoked)
            throw new InvalidOperationException("El token ya ha sido revocado");

        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = reason ?? "Token revocado manualmente";
    }

    public void RevokeIfExpired()
    {
        if (IsExpired && !IsRevoked)
        {
            Revoke("Token expirado");
        }
    }

    // Properties calculadas
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActive => !IsExpired && !IsRevoked;

    public TimeSpan TimeUntilExpiry => IsExpired ? TimeSpan.Zero : ExpiresAt - DateTimeOffset.UtcNow;

    public TimeSpan Age => DateTimeOffset.UtcNow - CreatedAt;
}