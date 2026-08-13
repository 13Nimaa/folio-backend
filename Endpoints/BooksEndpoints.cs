using BooksProject.Data;
using BooksProject.Dtos;
using BooksProject.Models;
using BooksProject.Services;
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
            string sortBy = "newest",
            int page = 1,
            int pageSize = 10) =>
        {
            if (page < 1)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid pagination parameters.",
                    detail: "Page must be at least 1.");
            }

            if (pageSize is < 1 or > 100)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid pagination parameters.",
                    detail: "Page size must be between 1 and 100.");
            }

            sortBy = sortBy.Trim().ToLowerInvariant();

            if (sortBy is not ("newest" or "rating" or "price" or "title"))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid sort option.",
                    detail: "Sort by must be one of: newest, rating, price, title.");
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

            query = sortBy switch 
            {
                "rating" => query
                    .OrderByDescending(book => (double)book.Rating)
                    .ThenBy(book => book.Id),
                "price" => query
                    .OrderBy(book => (double)book.Price)
                    .ThenBy(book => book.Id),
                "title" => query
                    .OrderBy(book => book.Title)
                    .ThenBy(book => book.Id),
                _ => query
                    .OrderByDescending(book => book.PublishedDate)
                    .ThenBy(book => book.Id)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(book => new BookListDto(
                    book.Id,
                    book.Title,
                    book.Author,
                    book.Genre.Name,
                    book.Price,
                    book.CoverImage
            
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
                    book.Author,
                    book.CoverImage,
                
                    book.Language
                 
             
                ))
                .FirstOrDefaultAsync();

            return book is null
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Book not found.",
                    detail: $"No book with ID {id} exists.")
                : Results.Ok(book);
        })
        .WithName(GetBookEndpointName);

        group.MapPost("/", async (
         CreateBookDto newBook,
         AppDbContext dbContext, IImageService imageService) =>

     {
            string? coverImageUrl = null;

         if (!string.IsNullOrWhiteSpace(newBook.CoverImage))
         {
             coverImageUrl =
                 await imageService.UploadBase64ImageAsync(
                     newBook.CoverImage);
         }
         Book book = new()
         {
             Title = newBook.Title,
             Description = newBook.Description,
             Price = newBook.Price,
             PublishedDate = newBook.PublishedDate,

        
             Language = newBook.Language,
          
             CoverImage = coverImageUrl,

             GenreId = newBook.GenreId,
             Author = newBook.Author
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
                 b.Author,
                 b.CoverImage,
             
                 b.Language
            
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
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Book update failed.",
                    detail: $"No book with ID {id} exists.");
            }
            existingBook.Title = updateBook.Title;
            existingBook.Description = updateBook.Description;
            existingBook.Price = updateBook.Price;
            existingBook.PublishedDate = updateBook.PublishedDate;
            existingBook.GenreId = updateBook.GenreId;
            await dbContext.SaveChangesAsync();

            return Results.Ok();







        });
        group.MapDelete("/{id}", async (int id, AppDbContext dbContext) =>
        {
            var deletedCount = await dbContext.Books
                .Where(book => book.Id == id)
                .ExecuteDeleteAsync();

            return deletedCount == 0
                ? Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Book deletion failed.",
                    detail: $"No book with ID {id} exists.")
                : Results.NoContent();
        });


    }
}
