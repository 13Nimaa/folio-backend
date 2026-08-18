namespace BooksProject.Dtos;

public record ConversationDto(
    int Id,
    int CustomerId,
    int PublisherId,
    DateTime CreatedAt,
    DateTime? LastMessageAt
);