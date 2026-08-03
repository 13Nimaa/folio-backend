using System.ComponentModel.DataAnnotations;

namespace BooksProject.Dtos;

public sealed record UpdateBookDto(
    [Required, StringLength(50,ErrorMessage ="")] string Title,
    [StringLength(100)] string Description,
    [Range(0, 50)] decimal Price,
    DateOnly PublishedDate,
    [Range(1, int.MaxValue)] int GenreId);
