using System.ComponentModel.DataAnnotations;

namespace BooksProject.Dtos;

public sealed record SignupDto(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password);

public sealed record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponseDto(
    int UserId,
    string Email,
    string Role,
    string AccessToken,
    DateTimeOffset ExpiresAt);
