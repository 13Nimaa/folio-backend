public sealed record BookDetailsDto(
    int Id,
    string Title,
    string Description,
    decimal Price,
    DateOnly PublishedDate,
    string Genre,
    string Author,
    string? CoverImage,
    string? ISBN,
    string? Language,
    int Pages,
    bool InStock,
    int StockQuantity
);