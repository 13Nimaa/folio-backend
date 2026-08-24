using BooksProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        var connString = builder.Configuration
            .GetConnectionString("BookStore");

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            if (builder.Environment.IsProduction())
            {
                options.UseNpgsql(connString)
                   .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
            else
            {
                options.UseSqlite(connString);
            }

            options.UseSeeding((context, _) =>
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
            });
        });
    }
}