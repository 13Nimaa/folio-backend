namespace BooksProject.Dtos;

public record MessageDto(
    int Id,
    int ConversationId,
    int SenderId,
    string Content,
    int? BookId,
    DateTime SentAt,
    bool IsRead
);