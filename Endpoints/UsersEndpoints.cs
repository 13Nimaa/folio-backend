using System.Security.Claims;
using BooksProject.Authentication;
using BooksProject.Data;
using Microsoft.EntityFrameworkCore;
public static class UsersEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/user");

        group.MapGet("/books", async (
            ClaimsPrincipal user,
            AppDbContext dbContext) =>
        {
            var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var books = await dbContext.Books
                .AsNoTracking()
                .Where(book => book.CreatedByUserId == userId)
                .Select(book => new BookListDto(
                    book.Id,
                    book.Title,
                    book.Author,
                    book.Genre.Name,
                    book.Price,
                    book.CoverImage,
                    book.StockQuantity
                ))
                .ToListAsync();

            return Results.Ok(books);
        })
        .RequireAuthorization();
    }
}