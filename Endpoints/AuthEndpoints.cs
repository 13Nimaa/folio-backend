using System.Security.Claims;
using BooksProject.Authentication;
using BooksProject.Data;
using BooksProject.Dtos;
using BooksProject.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

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
                Token = refreshToken,
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
        user.Role
    ), token, refreshToken, expiresAt));
        });

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
                Token = refreshToken,
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
        user.Role
    ), token, refreshToken, expiresAt));
        });
        group.MapPost("/logout", async (
    LogoutRequestDto request,
    AppDbContext dbContext) =>
{
    var refreshToken = await dbContext.RefreshTokens
        .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

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
    var refreshToken = await dbContext.RefreshTokens
    .Include(rt => rt.User)
    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
    if (refreshToken is null || !refreshToken.IsActive)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Token refresh failed.",
            detail: "The refresh token is invalid, expired, or revoked.");
    }
    var (accessToken, expiresAt) =
        tokenService.CreateAccessToken(refreshToken.User);
    refreshToken.RevokedAt = DateTime.UtcNow;
    var newRefreshToken = tokenService.CreateRefreshToken();

    var newRefreshTokenEntity = new RefreshToken
    {
        Token = newRefreshToken,
        UserId = refreshToken.UserId,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = tokenService.RefreshTokenExpiresAt
    };

    dbContext.RefreshTokens.Add(newRefreshTokenEntity);
    await dbContext.SaveChangesAsync();
    return Results.Ok(new AuthResponseDto(
           new UserDto(
        refreshToken.User.Id,
        refreshToken.User.Name,
        refreshToken.User.Email,
        refreshToken.User.Role
    ),
     
        accessToken,
        newRefreshToken,
        expiresAt
    ));

});

        group.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new
        {
            UserId = principal.FindFirstValue(TokenService.SubClaim),
            Email = principal.FindFirstValue(TokenService.EmailClaim),
            Role = principal.FindFirstValue(TokenService.RoleClaim)
        })).RequireAuthorization(policy =>
    policy.RequireRole(UserRoles.Admin));
    }
}
