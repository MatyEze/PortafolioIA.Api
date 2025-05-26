using Domain.Entities;
using System.Security.Claims;

namespace Application.Interfaces;

/// <summary>
/// Servicio para manejo de tokens JWT
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Genera un Access Token para el usuario
    /// </summary>
    /// <param name="user">Usuario para el cual generar el token</param>
    /// <param name="additionalClaims">Claims adicionales opcionales</param>
    /// <returns>Token JWT como string</returns>
    string GenerateAccessToken(User user, Dictionary<string, string>? additionalClaims = null);

    /// <summary>
    /// Genera un Refresh Token
    /// </summary>
    /// <returns>Refresh token como string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Valida un Access Token y extrae los claims
    /// </summary>
    /// <param name="token">Token a validar</param>
    /// <returns>ClaimsPrincipal si es válido, null si no</returns>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Extrae el User ID de un token válido
    /// </summary>
    /// <param name="token">Token JWT</param>
    /// <returns>User ID si es válido, null si no</returns>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Extrae el email del usuario de un token válido
    /// </summary>
    /// <param name="token">Token JWT</param>
    /// <returns>Email si es válido, null si no</returns>
    string? GetEmailFromToken(string token);

    /// <summary>
    /// Extrae el rol del usuario de un token válido
    /// </summary>
    /// <param name="token">Token JWT</param>
    /// <returns>Rol del usuario si es válido, null si no</returns>
    UserRole? GetRoleFromToken(string token);

    /// <summary>
    /// Verifica si un token ha expirado
    /// </summary>
    /// <param name="token">Token a verificar</param>
    /// <returns>True si ha expirado, False si aún es válido</returns>
    bool IsTokenExpired(string token);

    /// <summary>
    /// Obtiene la fecha de expiración de un token
    /// </summary>
    /// <param name="token">Token JWT</param>
    /// <returns>Fecha de expiración si es válido, null si no</returns>
    DateTimeOffset? GetTokenExpiration(string token);

    /// <summary>
    /// Obtiene todos los claims de un token
    /// </summary>
    /// <param name="token">Token JWT</param>
    /// <returns>Diccionario con todos los claims</returns>
    Dictionary<string, string> GetAllClaims(string token);
}

/// <summary>
/// Resultado de la generación de tokens
/// </summary>
public class TokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiration { get; set; }
    public DateTimeOffset RefreshTokenExpiration { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } // Segundos hasta expiración
}

/// <summary>
/// Claims personalizados para JWT
/// </summary>
public static class CustomClaims
{
    public const string UserId = "user_id";
    public const string Email = "email";
    public const string FirstName = "first_name";
    public const string LastName = "last_name";
    public const string Role = "role";
    public const string Status = "status";
    public const string EmailVerified = "email_verified";
    public const string LastLogin = "last_login";
    public const string TokenId = "jti"; // JWT ID para tracking
    public const string IssuedAt = "iat";
    public const string NotBefore = "nbf";
    public const string Expiration = "exp";
    public const string Issuer = "iss";
    public const string Audience = "aud";
}