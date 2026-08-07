public sealed record BookListDto(
    int Id,
    string Title,
    string Author,
    string Genre,
    decimal Price,
    string? CoverImage,
    bool InStock
);