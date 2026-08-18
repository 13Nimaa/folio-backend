namespace BooksProject.Dtos;

public record CreateMessageDto(
    string Content,
    int? BookId
);