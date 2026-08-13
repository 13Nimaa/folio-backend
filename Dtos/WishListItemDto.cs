namespace BooksProject.Dtos;

public sealed record WishlistItemDto(
    int BookId,
    string Title,
    string genre,
    string? coverImage,
    string? Description,
    decimal price,
    DateOnly PublishedDate,
    DateTime AddedAt
);