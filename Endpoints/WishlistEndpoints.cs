using BooksProject.Authentication;
using BooksProject.Data;
using BooksProject.Dtos;
using BooksProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BooksProject.Endpoints;

public static class WishlistEndpoints
{
    public static void MapWishlistEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/wishlist")
            .RequireAuthorization();

        group.MapPost("/{bookId:int}", async (
            int bookId,
            ClaimsPrincipal user,
            AppDbContext dbContext) =>
        {
            var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var bookExists = await dbContext.Books
                .AnyAsync(book => book.Id == bookId);

            if (!bookExists)
            {
                return Results.NotFound(new
                {
                    message = "Book not found."
                });
            }

            var alreadyExists = await dbContext.WishlistItems
                .AnyAsync(w =>
                    w.UserId == userId &&
                    w.BookId == bookId);

            if (alreadyExists)
            {
                return Results.Conflict(new
                {
                    message = "Book is already in your wishlist."
                });
            }

            var wishlistItem = new WishlistItem
            {
                UserId = userId,
                BookId = bookId,
                AddedAt = DateTime.UtcNow
            };

            dbContext.WishlistItems.Add(wishlistItem);

            await dbContext.SaveChangesAsync();

            return Results.Ok(new
            {
                message = "Book added to wishlist successfully.",
                bookId = bookId
            });
        });
        group.MapDelete("/{bookId:int}", async (
    int bookId,
    ClaimsPrincipal user,
    AppDbContext dbContext) =>
{
    var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

    if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var wishlistItem = await dbContext.WishlistItems
        .FirstOrDefaultAsync(w =>
            w.UserId == userId &&
            w.BookId == bookId);

    if (wishlistItem is null)
    {
        return Results.NotFound(new
        {
            message = "Book is not in your wishlist."
        });
    }

    dbContext.WishlistItems.Remove(wishlistItem);

    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        message = "Book removed from wishlist successfully.",
        bookId = bookId
    });
});
        group.MapGet("/", async (
            ClaimsPrincipal user,
            AppDbContext dbContext) =>
        {
            var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var wishlistItems = await dbContext.WishlistItems
                .Where(w => w.UserId == userId)
                .Select(w => new WishlistItemDto
                (
                   w.BookId,
                   w.Book.Title,
                   w.Book.Genre.Name,
                   w.Book.CoverImage,
                   w.Book.Description,
                    w.Book.Price,
                     w.Book.PublishedDate,
                     w.AddedAt
                ))
                .ToListAsync();

            return Results.Ok(new
            {
                items = wishlistItems
            });
        });
    }
}