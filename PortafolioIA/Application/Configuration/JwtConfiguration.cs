

namespace Application.Configuration;

/// <summary>
/// Configuración para JWT tokens
/// </summary>
public class JwtConfiguration
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Clave secreta para firmar los tokens (debe ser >= 256 bits / 32 caracteres)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Emisor del token (tu aplicación)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Audiencia del token (quién puede usar este token)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Duración del Access Token en minutos
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Duración del Refresh Token en días
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Si se debe validar el emisor
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Si se debe validar la audiencia
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Si se debe validar el tiempo de vida
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Si se debe validar la clave de firma
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Tolerancia de tiempo para la validación (en segundos)
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 0;

    // Propiedades calculadas
    public TimeSpan AccessTokenExpiration => TimeSpan.FromMinutes(AccessTokenExpirationMinutes);
    public TimeSpan RefreshTokenExpiration => TimeSpan.FromDays(RefreshTokenExpirationDays);

    /// <summary>
    /// Valida que la configuración sea correcta
    /// </summary>
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SecretKey))
            errors.Add("JWT SecretKey es requerida");
        else if (SecretKey.Length < 32)
            errors.Add("JWT SecretKey debe tener al menos 32 caracteres");

        if (string.IsNullOrWhiteSpace(Issuer))
            errors.Add("JWT Issuer es requerido");

        if (string.IsNullOrWhiteSpace(Audience))
            errors.Add("JWT Audience es requerida");

        if (AccessTokenExpirationMinutes <= 0)
            errors.Add("AccessTokenExpirationMinutes debe ser mayor a 0");

        if (RefreshTokenExpirationDays <= 0)
            errors.Add("RefreshTokenExpirationDays debe ser mayor a 0");

        if (AccessTokenExpirationMinutes > RefreshTokenExpirationDays * 24 * 60)
            errors.Add("AccessToken no puede durar más que RefreshToken");

        if (errors.Any())
        {
            throw new InvalidOperationException($"Configuración JWT inválida: {string.Join(", ", errors)}");
        }
    }
}