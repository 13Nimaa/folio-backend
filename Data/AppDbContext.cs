// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using BooksProject.Models;

namespace BooksProject.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }
    public DbSet<Genre> Genres { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ============================================================
        // Primary Keys / PostgreSQL Identity Columns
        // ============================================================

        modelBuilder.Entity<Book>()
            .HasKey(b => b.Id);

        modelBuilder.Entity<Book>()
            .Property(b => b.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Genre>()
            .HasKey(g => g.Id);

        modelBuilder.Entity<Genre>()
            .Property(g => g.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Author>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<Author>()
            .Property(a => a.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Order>()
            .HasKey(o => o.Id);

        modelBuilder.Entity<Order>()
            .Property(o => o.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Conversation>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Conversation>()
            .Property(c => c.Id)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Message>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<Message>()
            .Property(m => m.Id)
            .ValueGeneratedOnAdd();

        // ============================================================
        // Refresh Tokens
        // ============================================================

        // Token lookups are performed by value on every refresh.
        // Unique index makes them indexed and prevents duplicates.
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        // ============================================================
        // Book -> Genre
        // ============================================================

        modelBuilder.Entity<Book>()
            .HasOne(book => book.Genre)
            .WithMany(genre => genre.Books)
            .HasForeignKey(book => book.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // Book -> CreatedByUser
        // ============================================================

        modelBuilder.Entity<Book>()
            .HasOne(book => book.CreatedByUser)
            .WithMany()
            .HasForeignKey(book => book.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // User
        // ============================================================

        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique();

        // ============================================================
        // Wishlist
        // ============================================================

        modelBuilder.Entity<WishlistItem>()
            .HasKey(w => new
            {
                w.UserId,
                w.BookId
            });

        modelBuilder.Entity<WishlistItem>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId);

        modelBuilder.Entity<WishlistItem>()
            .HasOne(w => w.Book)
            .WithMany()
            .HasForeignKey(w => w.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============================================================
        // Orders
        // ============================================================

        modelBuilder.Entity<OrderItem>()
            .HasKey(x => new
            {
                x.OrderId,
                x.BookId
            });

        modelBuilder.Entity<Order>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Book)
            .WithMany()
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // Conversations
        // ============================================================

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            entity.HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Publisher)
                .WithMany()
                .HasForeignKey(c => c.PublisherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => new
            {
                c.CustomerId,
                c.PublisherId
            })
            .IsUnique();
        });

        // ============================================================
        // Messages
        // ============================================================

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Id)
                .ValueGeneratedOnAdd();

            entity.Property(m => m.Content)
                .IsRequired()
                .HasMaxLength(4000);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Book)
                .WithMany()
                .HasForeignKey(m => m.BookId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(m => new
            {
                m.ConversationId,
                m.SentAt
            });
        });
    }
}