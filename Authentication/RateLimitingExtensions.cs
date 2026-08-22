using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace BooksProject.Authentication;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    // Brute-force guard for credential endpoints. Keyed by remote IP: 10
    // attempts per minute is far above human traffic and far below what a
    // password spray needs.
    public static void AddAuthRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
    }
}
