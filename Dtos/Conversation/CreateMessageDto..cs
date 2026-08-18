using System.ComponentModel.DataAnnotations;

namespace BooksProject.Dtos;

public record CreateMessageDto(
  [Required] [MaxLength(4000)]  string Content,
    int? BookId
);