using System.ComponentModel.DataAnnotations;

namespace BooksProject.Dtos;

// Limits mirror CreateBookDto so a book can never be valid at creation but
// impossible to save again after an edit. Author/Language/CoverImage are
// optional: null leaves the stored value untouched.
public sealed record UpdateBookDto(
    [Required, StringLength(100)]
    string Title,

    [Required, StringLength(1000)]
    string Description,

    [Range(0, 10000)]
    decimal Price,

    DateOnly PublishedDate,

    [Range(1, int.MaxValue)]
    int GenreId,

    [Range(0, int.MaxValue)]
    int StockQuantity,

    [StringLength(200)]
    string? Author = null,

    [StringLength(50)]
    string? Language = null,

    string? CoverImage = null
);
