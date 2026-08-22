using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BooksProject.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BooksProject.Authentication;

public class TokenService(IOptions<JwtOptions> options)
{
    // Claim names are emitted and read as-is: JwtBearer is configured with
    // MapInboundClaims = false, so no XML-URI remapping happens on validation.
    public const string SubClaim = "sub";
    public const string EmailClaim = "email";
    public const string RoleClaim = "role";

    private readonly JwtOptions jwtOptions = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.ExpiryMinutes);

        Claim[] claims =
        [
            new(SubClaim, user.Id.ToString()),
            new(EmailClaim, user.Email),
            new(RoleClaim, user.Role),
            new("jti", Guid.NewGuid().ToString())
        ];

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.Key));

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
    public string CreateRefreshToken()
{
    var bytes = RandomNumberGenerator.GetBytes(64);

    return Convert.ToBase64String(bytes);
}

    // Refresh tokens are stored hashed (SHA-256): a database leak must not
    // yield usable session tokens. Lookup always hashes the presented value.
    public static string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(bytes);
    }

    public DateTime RefreshTokenExpiresAt =>
        DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenExpiryDays);
}
