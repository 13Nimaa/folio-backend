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
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasOne(book => book.Genre)
            .WithMany(genre => genre.Books)
            .HasForeignKey(book => book.GenreId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Book>()
            .HasOne(b => b.CreatedByUser)
            .WithMany()
            .HasForeignKey(b => b.CreatedByUserId)
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
            .HasForeignKey(w => w.BookId)
            // Wishlist entries are pointers; removing a book cleans them up.
            // (OrderItems above are Restrict — history is protected.)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderItem>()
.HasKey(x => new { x.OrderId, x.BookId });

        modelBuilder.Entity<Order>()
            .HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Book)
            .WithMany()
            .HasForeignKey(x => x.BookId)
            // Order history must never be silently rewritten by a book deletion.
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Conversation>(entity =>
{
    entity.HasKey(c => c.Id);

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
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

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
