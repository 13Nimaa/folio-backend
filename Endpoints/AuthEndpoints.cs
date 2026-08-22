using System.Security.Claims;
using BooksProject.Authentication;
using BooksProject.Data;
using BooksProject.Dtos;
using BooksProject.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using BooksProject.Services;

namespace BooksProject.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/signup", async (
            SignupDto signup,
            IValidator<SignupDto> validator,
            AppDbContext dbContext,
            TokenService tokenService) =>
        {
            var validationResult = await validator.ValidateAsync(signup);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var email = signup.Email.Trim().ToLowerInvariant();

            var emailTaken = await dbContext.Users
                .AnyAsync(user => user.Email == email);

            if (emailTaken)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Signup failed.",
                    detail: "An account with that email already exists.");
            }

            User user = new()
            {
                Name = signup.name,
                Email = email,
                PasswordHash = PasswordHasher.Hash(signup.Password),
                Role = UserRoles.User
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var (token, expiresAt) = tokenService.CreateAccessToken(user);
            var refreshToken = tokenService.CreateRefreshToken();

            RefreshToken refreshTokenEntity = new()
            {
                Token = TokenService.HashRefreshToken(refreshToken),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = tokenService.RefreshTokenExpiresAt
            };

            dbContext.RefreshTokens.Add(refreshTokenEntity);
            await dbContext.SaveChangesAsync();
            return Results.Created(
                "/auth/me",
                new AuthResponseDto(new UserDto(
        user.Id,
        user.Name,
        user.Email,
        user.ProfileImage,
        user.Role
    ), token, refreshToken, expiresAt));
        }).RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

        group.MapPost("/login", async (
            LoginDto login,
            AppDbContext dbContext,
            TokenService tokenService) =>
        {
            var email = login.Email.Trim().ToLowerInvariant();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(user => user.Email == email);

            // Same response for unknown email and wrong password so the endpoint
            // does not reveal which accounts exist.
            if (user is null || !PasswordHasher.Verify(login.Password, user.PasswordHash))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Login failed.",
                    detail: "Invalid email or password.");
            }
            var refreshToken = tokenService.CreateRefreshToken();

            var (token, expiresAt) = tokenService.CreateAccessToken(user);
            RefreshToken refreshTokenEntity = new()
            {
                Token = TokenService.HashRefreshToken(refreshToken),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = tokenService.RefreshTokenExpiresAt
            };
            dbContext.RefreshTokens.Add(refreshTokenEntity);

            await dbContext.SaveChangesAsync();

            return Results.Ok(
                new AuthResponseDto(new UserDto(
        user.Id,
        user.Name,
        user.Email,
                user.ProfileImage,

        user.Role
    ), token, refreshToken, expiresAt));
        }).RequireRateLimiting(RateLimitingExtensions.AuthPolicy);
        group.MapPost("/logout", async (
    LogoutRequestDto request,
    AppDbContext dbContext) =>
{
    var tokenHash = TokenService.HashRefreshToken(request.RefreshToken ?? "");

    var refreshToken = await dbContext.RefreshTokens
        .FirstOrDefaultAsync(rt => rt.Token == tokenHash);

    if (refreshToken is not null)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        Message = "Logged out successfully."
    });
});
        group.MapPost("/refresh", async (
    RefreshTokenRequestDto request,
    AppDbContext dbContext,
    TokenService tokenService) =>
{
    var tokenHash = TokenService.HashRefreshToken(request.RefreshToken ?? "");

    var stored = await dbContext.RefreshTokens
        .AsNoTracking()
        .FirstOrDefaultAsync(rt => rt.Token == tokenHash);

    if (stored is null || !stored.IsActive)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Token refresh failed.",
            detail: "The refresh token is invalid, expired, or revoked.");
    }

    // Atomic single-use claim: the UPDATE only succeeds while RevokedAt is
    // still null, so of N concurrent refreshes with one token exactly one
    // wins; the rest are treated as replay and rejected.
    var claimed = await dbContext.RefreshTokens
        .Where(rt => rt.Id == stored.Id &&
            rt.RevokedAt == null &&
            rt.ExpiresAt > DateTime.UtcNow)
        .ExecuteUpdateAsync(s =>
            s.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));

    if (claimed == 0)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Token refresh failed.",
            detail: "The refresh token is invalid, expired, or revoked.");
    }

    var user = await dbContext.Users.FindAsync(stored.UserId);

    if (user is null)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Token refresh failed.",
            detail: "The refresh token is invalid, expired, or revoked.");
    }

    var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
    var newRefreshToken = tokenService.CreateRefreshToken();

    dbContext.RefreshTokens.Add(new RefreshToken
    {
        Token = TokenService.HashRefreshToken(newRefreshToken),
        UserId = stored.UserId,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = tokenService.RefreshTokenExpiresAt
    });

    await dbContext.SaveChangesAsync();

    return Results.Ok(new AuthResponseDto(
           new UserDto(
        user.Id,
        user.Name,
        user.Email,
        user.ProfileImage,
        user.Role
    ),

        accessToken,
        newRefreshToken,
        expiresAt
    ));

}).RequireRateLimiting(RateLimitingExtensions.AuthPolicy);

        group.MapPut("/me", async (
            UpdateProfileDto update,
            ClaimsPrincipal principal,
            AppDbContext dbContext,
            IImageService imageService) =>
        {
            var userIdClaim = principal.FindFirstValue(TokenService.SubClaim);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users.FindAsync(userId);

            if (user is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Profile update failed.",
                    detail: "User not found.");
            }

            if (!string.IsNullOrWhiteSpace(update.Name))
            {
                user.Name = update.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(update.ProfileImage))
            {
                user.ProfileImage = await imageService.UploadBase64ImageAsync(
                    update.ProfileImage);
            }

            await dbContext.SaveChangesAsync();

            return Results.Ok(new UserDto(
                user.Id,
                user.Name,
                user.Email,
                user.ProfileImage,
                user.Role));
        }).RequireAuthorization();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            AppDbContext dbContext) =>
        {
            var userIdClaim = principal.FindFirstValue(TokenService.SubClaim);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Results.Unauthorized();
            }

            var user = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserDto(
                    u.Id,
                    u.Name,
                    u.Email,
                    u.ProfileImage,
                    u.Role))
                .FirstOrDefaultAsync();

            return user is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Profile not found.",
                    detail: "User not found.")
                : Results.Ok(user);
        }).RequireAuthorization();
    }
}
