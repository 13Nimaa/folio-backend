using System.Security.Claims;
using BooksProject.Authentication;
using BooksProject.Data;
using BooksProject.Dtos;
using BooksProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BooksProject.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/signup", async (
            SignupDto signup,
            AppDbContext dbContext,
            TokenService tokenService) =>
        {
            var email = signup.Email.Trim().ToLowerInvariant();

            var emailTaken = await dbContext.Users
                .AnyAsync(user => user.Email == email);

            if (emailTaken)
            {
                return Results.Conflict(new
                {
                    Message = "An account with that email already exists."
                });
            }

            User user = new()
            {
                Email = email,
                PasswordHash = PasswordHasher.Hash(signup.Password),
                Role = UserRoles.User
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var (token, expiresAt) = tokenService.CreateAccessToken(user);

            return Results.Created(
                "/auth/me",
                new AuthResponseDto(user.Id, user.Email, user.Role, token, expiresAt));
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
                return Results.Json(
                    new { Message = "Invalid email or password." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var (token, expiresAt) = tokenService.CreateAccessToken(user);

            return Results.Ok(
                new AuthResponseDto(user.Id, user.Email, user.Role, token, expiresAt));
        });

        group.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new
        {
            UserId = principal.FindFirstValue(TokenService.SubClaim),
            Email = principal.FindFirstValue(TokenService.EmailClaim),
            Role = principal.FindFirstValue(TokenService.RoleClaim)
        })).RequireAuthorization();
    }
}
