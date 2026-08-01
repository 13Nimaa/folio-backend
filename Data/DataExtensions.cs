using BooksProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BooksProject.Data;

public static class DataExtensions
{
    public static void MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        dbContext.Database.Migrate();
    }
    public static void AddAppStoreDb(this WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("BookStore");

        builder.Services.AddSqlite<AppDbContext>(
            connString,
            optionsAction: options => options.UseSeeding((context, _) =>
            {
                string[] genreNames =
                [
                    "Fiction",
                    "Non-Fiction",
                    "Mystery",
                    "Science Fiction",
                    "Fantasy",
                    "Romance",
                    "Horror",
                    "Biography",
                    "History",
                    "Poetry",
                    "Self-Help",
                    "Thriller"
                ];

                var existingNames = context.Set<Genre>()
                    .Select(genre => genre.Name)
                    .ToHashSet();

                var newGenres = genreNames
                    .Where(name => !existingNames.Contains(name))
                    .Select(name => new Genre { Name = name });

                if (newGenres.Any())
                {
                    context.Set<Genre>().AddRange(newGenres);
                    context.SaveChanges();
                }
            })
        );
    }
}
