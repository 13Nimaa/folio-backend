using BooksProject.Models;

public class WishlistItem
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public DateTime AddedAt { get; set; }
}