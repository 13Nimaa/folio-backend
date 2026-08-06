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
                .OrderBy(book => book.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(book => new BookListDto(
                       book.Id,
        book.Title,
        book.Author.Name,
        book.Genre.Name,
        book.Price,
        book.CoverImageUrl,
        book.StockQuantity > 0
                ))
                .ToListAsync();

            var response = new PagedResponse<BookListDto>(
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
            var book = await dbContext.Books
                .AsNoTracking()
                .Where(book => book.Id == id)
                .Select(book => new BookDetailsDto(
                    book.Id,
                    book.Title,
                    book.Description,
                    book.Price,
                    book.PublishedDate,
                    book.Genre.Name,
                    book.Author.Name,
                    book.CoverImageUrl,
                    book.ISBN,
                    book.Language,
                    book.Pages,
                    book.StockQuantity > 0,
                    book.StockQuantity
                ))
                .FirstOrDefaultAsync();

            return book is null
                ? Results.NotFound()
                : Results.Ok(book);
        })
        .WithName(GetBookEndpointName);

        group.MapPost("/", async (
         CreateBookDto newBook,
         AppDbContext dbContext) =>
     {
         Book book = new()
         {
             Title = newBook.Title,
             Description = newBook.Description,
             Price = newBook.Price,
             PublishedDate = newBook.PublishedDate,

             ISBN = newBook.ISBN,
             Language = newBook.Language,
             Pages = newBook.Pages,
             StockQuantity = newBook.StockQuantity,
             CoverImageUrl = newBook.CoverImageUrl,

             GenreId = newBook.GenreId,
             AuthorId = newBook.AuthorId
         };

         dbContext.Books.Add(book);

         await dbContext.SaveChangesAsync();


         var createdBook = await dbContext.Books
             .AsNoTracking()
             .Where(b => b.Id == book.Id)
             .Select(b => new BookDetailsDto(
                 b.Id,
                 b.Title,
                 b.Description,
                 b.Price,
                 b.PublishedDate,
                 b.Genre.Name,
                 b.Author.Name,
                 b.CoverImageUrl,
                 b.ISBN,
                 b.Language,
                 b.Pages,
                 b.StockQuantity > 0,
                 b.StockQuantity
             ))
             .FirstAsync();


         return Results.CreatedAtRoute(
             GetBookEndpointName,
             new { id = book.Id },
             createdBook
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
            dbContext.Books.Where(book => book.Id == id).ExecuteDeleteAsync();
            return Results.Ok();

        });


    }
}
