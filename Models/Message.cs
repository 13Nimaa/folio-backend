using BooksProject.Models;

public class Message
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public int? BookId { get; set; }
    public Book? Book { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
}