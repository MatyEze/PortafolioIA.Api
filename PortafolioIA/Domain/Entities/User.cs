using Domain.ValueObjects;

namespace Domain.Entities;

public enum UserRole
{
    Usuario = 1,
    Premium = 2,
    Admin = 3,
    SuperAdmin = 4
}

public enum UserStatus
{
    Pending = 1,    // Pendiente de verificación
    Active = 2,     // Activo
    Suspended = 3,  // Suspendido temporalmente
    Banned = 4      // Baneado permanentemente
}

public class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? EmailVerifiedAt { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTimeOffset? PasswordResetTokenExpiry { get; private set; }
    public string? PasswordResetToken { get; private set; }

    // Navigation properties
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<DataPoint> _dataPoints = new();
    public IReadOnlyCollection<DataPoint> DataPoints => _dataPoints.AsReadOnly();

    // Constructor privado para EF Core
    private User() { }

    // Factory method para crear nuevo usuario
    public static User Create(
        Email email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role = UserRole.Usuario)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("El nombre no puede estar vacío", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("El apellido no puede estar vacío", nameof(lastName));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de la contraseña no puede estar vacío", nameof(passwordHash));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            Status = UserStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            EmailVerificationToken = GenerateVerificationToken()
        };

        return user;
    }

    // Métodos de negocio
    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("El nombre no puede estar vacío", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("El apellido no puede estar vacío", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("El hash de la contraseña no puede estar vacío", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Invalidar todos los refresh tokens existentes
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    public void VerifyEmail()
    {
        if (EmailVerifiedAt.HasValue)
            throw new InvalidOperationException("El email ya ha sido verificado");

        EmailVerifiedAt = DateTimeOffset.UtcNow;
        EmailVerificationToken = null;
        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        if (Status == UserStatus.Banned)
            throw new InvalidOperationException("No se puede suspender un usuario baneado");

        Status = UserStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Revocar todos los refresh tokens
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    public void Ban()
    {
        Status = UserStatus.Banned;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Revocar todos los refresh tokens
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    public void Activate()
    {
        if (Status == UserStatus.Banned)
            throw new InvalidOperationException("Un usuario baneado no puede ser activado directamente");

        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void GeneratePasswordResetToken()
    {
        PasswordResetToken = GenerateVerificationToken();
        PasswordResetTokenExpiry = DateTimeOffset.UtcNow.AddHours(2); // Válido por 2 horas
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ResetPassword(string newPasswordHash, string resetToken)
    {
        if (string.IsNullOrWhiteSpace(resetToken) || PasswordResetToken != resetToken)
            throw new InvalidOperationException("Token de reseteo inválido");

        if (!PasswordResetTokenExpiry.HasValue || PasswordResetTokenExpiry < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Token de reseteo expirado");

        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Revocar todos los refresh tokens existentes
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    public void AddRefreshToken(RefreshToken refreshToken)
    {
        _refreshTokens.Add(refreshToken);
    }

    public void RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(t => t.Token == token);
        refreshToken?.Revoke();
    }

    // Métodos helper
    public string GetFullName() => $"{FirstName} {LastName}";

    public bool IsEmailVerified => EmailVerifiedAt.HasValue;

    public bool IsActive => Status == UserStatus.Active;

    public bool CanLogin => IsActive && IsEmailVerified;

    public bool HasRole(UserRole role) => Role >= role;

    private static string GenerateVerificationToken()
    {
        return Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
    }
}