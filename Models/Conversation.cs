using BooksProject.Models;

public class Conversation
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public int PublisherId { get; set; }
    public User Publisher { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}