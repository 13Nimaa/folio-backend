namespace BooksProject.Dtos;

public sealed record BookDetailsDto(
    int Id,
    string Title,
    string? Description,
    decimal Price,
    DateOnly PublishedDate,
    int GenreId);
