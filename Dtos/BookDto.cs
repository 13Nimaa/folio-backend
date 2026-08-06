public sealed record BookListDto(
    int Id,
    string Title,
    string Author,
    string Genre,
    decimal Price,
    string? CoverImageUrl,
    bool InStock
);