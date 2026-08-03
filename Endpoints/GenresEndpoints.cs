using BooksProject.Data;
using BooksProject.Dtos;
using Microsoft.EntityFrameworkCore;
namespace GameStore.Api.Endpoints;

public static class GenresEndpoints{
    public static void MapGenresEndpoint(this WebApplication app)
    {
                var group = app.MapGroup("/genres");
                group.MapGet("/", async (AppDbContext dbContext) =>
                    await dbContext.Genres
                        .AsNoTracking()
                        .Select(genre => new GenreDto(genre.Id, genre.Name))
                        .ToListAsync()
                );

    }
}
