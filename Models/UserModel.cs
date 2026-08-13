namespace BooksProject.Models;

public class User
{
    public int Id { get; set; }

    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string PasswordHash { get; set; }
    public string ProfileImage { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
}
