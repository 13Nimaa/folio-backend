using BooksProject.Data;
using BooksProject.Dtos;
using BooksProject.Models;
using Microsoft.EntityFrameworkCore;
namespace BooksProject.Endpoints;

public static class BooksEndpoints
{
    private const string GetBookEndpointName = "GetBook";

    public static void MapBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/books");
        group.MapGet("/", async (
            string? search,
            int? genreId,
            decimal? minPrice,
            decimal? maxPrice,
            DateOnly? publishedAfter,
            DateOnly? publishedBefore,
            AppDbContext dbContext,
            int page = 1,
            int pageSize = 10) =>
        {
            if (page < 1)
            {
                return Results.BadRequest(new { Message = "Page must be at least 1." });
            }

            if (pageSize is < 1 or > 100)
            {
                return Results.BadRequest(new
                {
                    Message = "Page size must be between 1 and 100."
                });
            }

            var query = dbContext.Books.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string pattern = $"%{search.Trim()}%";

                query = query.Where(book =>
                    EF.Functions.Like(book.Title, pattern) ||
                    EF.Functions.Like(book.Description, pattern));
            }

            if (genreId.HasValue)
            {
                query = query.Where(book => book.GenreId == genreId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(book => book.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(book => book.Price <= maxPrice.Value);
            }

            if (publishedAfter.HasValue)
            {
                query = query.Where(book =>
                    book.PublishedDate >= publishedAfter.Value);
            }

            if (publishedBefore.HasValue)
            {
                query = query.Where(book =>
                    book.PublishedDate <= publishedBefore.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var books = await query
                .OrderBy(book => book.id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(book => new BookDto(
                    book.id,
                    book.Title,
                    book.Description,
                    book.Price,
                    book.PublishedDate,
                    book.Genre.Name
                ))
                .ToListAsync();

            var response = new PagedResponse<BookDto>(
                books,
                page,
                pageSize,
                totalCount,
                totalPages,
                page > 1,
                page < totalPages);

            return Results.Ok(response);
        });
        group.MapGet("/{id}", async (int id, AppDbContext dbContext) =>
    {
        var book = await dbContext.Books.FindAsync(id);
        return book is null ? Results.NotFound() : Results.Ok(
           new BookDetailsDto(
            book.id,
            book.Title,
            book.Description,
      book.Price,
           book.PublishedDate,
            book.GenreId




            )
        );
    }).WithName(GetBookEndpointName);

        group.MapPost("/", async (CreateBookDto newBook, AppDbContext dbContext) =>
        {
            Book book = new()
            {

                Title = newBook.Title,
                Description = newBook.Description,
                GenreId = newBook.GenreId,
                Price = newBook.Price,
                PublishedDate = newBook.PublishedDate
            };


            dbContext.Books.Add(book);
            await dbContext.SaveChangesAsync();
            BookDetailsDto BookDto = new(
             book.id,
             book.Title,
        book.Description,
             book.Price,
             book.PublishedDate,
                  book.GenreId
            );






            return Results.CreatedAtRoute(
                GetBookEndpointName,
                new { id = book.id },
                BookDto
            );
        });
        group.MapPut("/{id}", async (int id, UpdateBookDto updateBook, AppDbContext dbContext) =>

        {
            var existingBook = await dbContext.Books.FindAsync(id);
            if (existingBook is null)
            {
                return Results.NotFound();
            }
            existingBook.Title = updateBook.Title;
            existingBook.Description = updateBook.Description;
            existingBook.Price = updateBook.Price;
            existingBook.PublishedDate = updateBook.PublishedDate;
            existingBook.GenreId = updateBook.GenreId;
            await dbContext.SaveChangesAsync();

            return Results.Ok();







        });
        group.MapDelete("/{id}", (int id, AppDbContext dbContext) =>
        {
            dbContext.Books.Where(book => book.id == id).ExecuteDeleteAsync();
            return Results.Ok();

        });


    }
}
