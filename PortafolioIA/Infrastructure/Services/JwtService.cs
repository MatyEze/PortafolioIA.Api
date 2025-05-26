using Application.Interfaces;
using Application.Configuration;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly JwtConfiguration _jwtConfig;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtService(IOptions<JwtConfiguration> jwtOptions)
    {
        _jwtConfig = jwtOptions.Value;
        _jwtConfig.Validate(); // Validar configuración al inicio

        _tokenHandler = new JwtSecurityTokenHandler();

        // Configurar credenciales de firma
        var secretKey = Encoding.UTF8.GetBytes(_jwtConfig.SecretKey);
        var securityKey = new SymmetricSecurityKey(secretKey);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Configurar parámetros de validación
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = _jwtConfig.ValidateIssuer,
            ValidateAudience = _jwtConfig.ValidateAudience,
            ValidateLifetime = _jwtConfig.ValidateLifetime,
            ValidateIssuerSigningKey = _jwtConfig.ValidateIssuerSigningKey,

            ValidIssuer = _jwtConfig.Issuer,
            ValidAudience = _jwtConfig.Audience,
            IssuerSigningKey = securityKey,

            ClockSkew = TimeSpan.FromSeconds(_jwtConfig.ClockSkewSeconds),

            // Requerir fecha de expiración
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };
    }

    public string GenerateAccessToken(User user, Dictionary<string, string>? additionalClaims = null)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var now = DateTimeOffset.UtcNow;
        var expiration = now.Add(_jwtConfig.AccessTokenExpiration);

        // Claims básicos del usuario
        var claims = new List<Claim>
        {
            new(CustomClaims.UserId, user.Id.ToString()),
            new(CustomClaims.Email, user.Email.Value),
            new(CustomClaims.FirstName, user.FirstName),
            new(CustomClaims.LastName, user.LastName),
            new(CustomClaims.Role, user.Role.ToString()),
            new(CustomClaims.Status, user.Status.ToString()),
            new(CustomClaims.EmailVerified, user.IsEmailVerified.ToString().ToLower()),
            new(CustomClaims.TokenId, Guid.NewGuid().ToString()), // Unique token ID
            new(CustomClaims.IssuedAt, now.ToUnixTimeSeconds().ToString()),
            new(CustomClaims.NotBefore, now.ToUnixTimeSeconds().ToString()),
            
            // Claims estándar
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Agregar último login si existe
        if (user.LastLoginAt.HasValue)
        {
            claims.Add(new Claim(CustomClaims.LastLogin, user.LastLoginAt.Value.ToUnixTimeSeconds().ToString()));
        }

        // Agregar claims adicionales si se proporcionan
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        // Crear el token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration.DateTime,
            Issuer = _jwtConfig.Issuer,
            Audience = _jwtConfig.Audience,
            SigningCredentials = _signingCredentials,
            NotBefore = now.DateTime
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Generar un token aleatorio seguro de 64 bytes
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            // Verificar que sea un JWT válido
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateAccessToken(token);
        var userIdClaim = principal?.FindFirst(CustomClaims.UserId)?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public string? GetEmailFromToken(string token)
    {
        var principal = ValidateAccessToken(token);
        return principal?.FindFirst(CustomClaims.Email)?.Value;
    }

    public UserRole? GetRoleFromToken(string token)
    {
        var principal = ValidateAccessToken(token);
        var roleClaim = principal?.FindFirst(CustomClaims.Role)?.Value;

        return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : null;
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true; // Si no se puede leer, considerar expirado
        }
    }

    public DateTimeOffset? GetTokenExpiration(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return new DateTimeOffset(jwtToken.ValidTo, TimeSpan.Zero);
        }
        catch
        {
            return null;
        }
    }

    public Dictionary<string, string> GetAllClaims(string token)
    {
        var claims = new Dictionary<string, string>();

        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            foreach (var claim in jwtToken.Claims)
            {
                claims[claim.Type] = claim.Value;
            }
        }
        catch
        {
            // Token inválido, devolver diccionario vacío
        }

        return claims;
    }
}

/// <summary>
/// Extensiones para trabajar con JWT más fácilmente
/// </summary>
public static class JwtExtensions
{
    /// <summary>
    /// Extrae el token Bearer del header Authorization
    /// </summary>
    public static string? ExtractBearerToken(this string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;

        const string bearerPrefix = "Bearer ";
        return authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[bearerPrefix.Length..]
            : null;
    }

    /// <summary>
    /// Convierte DateTimeOffset a Unix timestamp
    /// </summary>
    public static long ToUnixTimeSeconds(this DateTimeOffset dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Convierte Unix timestamp a DateTimeOffset
    /// </summary>
    public static DateTimeOffset FromUnixTimeSeconds(long unixTime)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTime);
    }
}