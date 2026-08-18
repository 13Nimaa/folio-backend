namespace BooksProject.Dtos;

public record ConversationDto(
    int Id,
    int CustomerId,
    int PublisherId,
    int OtherUserId,
    string OtherUserName,
    string OtherUserProfileImage,
    string? LastMessage,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    int UnreadCount
);