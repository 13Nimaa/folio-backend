namespace BooksProject.Dtos;

public sealed record BookDto(
    int Id,
    string Title,
    string? Description,
    decimal Price,
    DateOnly PublishedDate,
    string Genre);
