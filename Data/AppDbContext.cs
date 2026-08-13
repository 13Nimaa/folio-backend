// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using BooksProject.Models;

namespace BooksProject.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WishlistItem> WishlistItems { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasOne(book => book.Genre)
            .WithMany(genre => genre.Books)
            .HasForeignKey(book => book.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();
        modelBuilder.Entity<WishlistItem>()
.HasKey(w => new { w.UserId, w.BookId });
        modelBuilder.Entity<WishlistItem>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId);

        modelBuilder.Entity<WishlistItem>()
            .HasOne(w => w.Book)
            .WithMany()
            .HasForeignKey(w => w.BookId);
    }
}
