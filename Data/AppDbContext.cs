// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using BooksProject.Models;

namespace BooksProject.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasOne(book => book.Genre)
            .WithMany()
            .HasForeignKey(book => book.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();
    }
}
