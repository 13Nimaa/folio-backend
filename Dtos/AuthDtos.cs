using System.ComponentModel.DataAnnotations;

namespace BooksProject.Dtos;

public sealed record SignupDto(
    string name,
     string Email,
   string Password,
   string confirmPassword);

public sealed record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponseDto(
    int UserId,
    string Email,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
