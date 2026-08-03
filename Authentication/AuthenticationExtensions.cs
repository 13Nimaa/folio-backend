using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BooksProject.Authentication;

public static class AuthenticationExtensions
{
    public static void AddJwtAuthentication(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = section.Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                $"Missing '{JwtOptions.SectionName}' configuration section.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be configured and at least 32 characters long.");
        }

        builder.Services.Configure<JwtOptions>(section);
        builder.Services.AddSingleton<TokenService>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep the short claim names ("sub", "email", "role") instead of
                // letting them be remapped to the legacy XML URI claim types.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = TokenService.EmailClaim,
                    RoleClaimType = TokenService.RoleClaim
                };
            });

        builder.Services.AddAuthorization();
    }
}
