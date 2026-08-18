using System.ComponentModel.DataAnnotations;

namespace BooksProject.Dtos;

public sealed record CreateBookDto(
    [Required, StringLength(100)]
    string Title,

    [Required, StringLength(1000)]
    string Description,

    [Range(0, 10000)]
    decimal Price,

    DateOnly PublishedDate,



    [Required, StringLength(50)]
    string Language,

 



    string? CoverImage,

    [Range(1, int.MaxValue)]
    int GenreId,

    [Required, StringLength(200)]
    string Author,

    [Range(0, int.MaxValue)]
    int StockQuantity
);