using System.Security.Claims;
using BooksProject.Authentication;
using BooksProject.Data;
using BooksProject.Dtos;
using BooksProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/conversations")
            .RequireAuthorization();

        // Get all conversations for the current user
        group.MapGet(
          "/",
          async (
              ClaimsPrincipal user,
              AppDbContext dbContext) =>
          {
              var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

              if (userIdClaim is null ||
                  !int.TryParse(userIdClaim, out var userId))
              {
                  return Results.Unauthorized();
              }

              var conversations = await dbContext.Conversations
                  .AsNoTracking()
                  .Where(c =>
                      c.CustomerId == userId ||
                      c.PublisherId == userId)
                  .OrderByDescending(c =>
                      c.LastMessageAt ?? c.CreatedAt)
                  .Select(c => new ConversationDto(
                      c.Id,
                      c.CustomerId,
                      c.PublisherId,

                      c.CustomerId == userId
                          ? c.PublisherId
                          : c.CustomerId,

                      c.CustomerId == userId
                          ? c.Publisher.Name
                          : c.Customer.Name,

                      c.CustomerId == userId
                          ? c.Publisher.ProfileImage
                          : c.Customer.ProfileImage,

                      c.Messages
                          .OrderByDescending(m => m.SentAt)
                          .Select(m => m.Content)
                          .FirstOrDefault(),

                      c.CreatedAt,
                      c.LastMessageAt,

                      c.Messages.Count(m =>
                          m.SenderId != userId &&
                          !m.IsRead)
                  ))
                  .ToListAsync();

              return Results.Ok(conversations);
          });
        // Get or create a conversation for a book
     group.MapPost(
    "/books/{bookId:int}",
    async (
        int bookId,
        ClaimsPrincipal user,
        AppDbContext dbContext) =>
    {
        var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

        if (userIdClaim is null ||
            !int.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var book = await dbContext.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book is null)
        {
            return Results.Problem(
                title: "Book not found",
                detail: $"No book was found with ID {bookId}.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (book.CreatedByUserId == userId)
        {
            return Results.Problem(
                title: "Invalid conversation",
                detail: "You cannot start a conversation with yourself.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var conversation = await dbContext.Conversations
            .Include(c => c.Publisher)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c =>
                c.CustomerId == userId &&
                c.PublisherId == book.CreatedByUserId);

        if (conversation is not null)
        {
            var existingConversation = new ConversationDto(
                conversation.Id,
                conversation.CustomerId,
                conversation.PublisherId,

                conversation.PublisherId,

                conversation.Publisher.Name,
                conversation.Publisher.ProfileImage,

                conversation.Messages
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content)
                    .FirstOrDefault(),

                conversation.CreatedAt,
                conversation.LastMessageAt,

                conversation.Messages.Count(m =>
                    m.SenderId != userId &&
                    !m.IsRead)
            );

            return Results.Ok(existingConversation);
        }

        conversation = new Conversation
        {
            CustomerId = userId,
            PublisherId = book.CreatedByUserId
        };

        dbContext.Conversations.Add(conversation);

        await dbContext.SaveChangesAsync();

        // Reload navigation properties and messages
        conversation = await dbContext.Conversations
            .AsNoTracking()
            .Include(c => c.Publisher)
            .Include(c => c.Messages)
            .FirstAsync(c => c.Id == conversation.Id);

        var response = new ConversationDto(
            conversation.Id,
            conversation.CustomerId,
            conversation.PublisherId,

            conversation.PublisherId,

            conversation.Publisher.Name,
            conversation.Publisher.ProfileImage,

            conversation.Messages
                .OrderByDescending(m => m.SentAt)
                .Select(m => m.Content)
                .FirstOrDefault(),

            conversation.CreatedAt,
            conversation.LastMessageAt,

            0
        );

        return Results.Created(
            $"/conversations/{conversation.Id}",
            response
        );
    });
        group.MapGet(
"/{conversationId:int}/messages",
async (
    int conversationId,
    ClaimsPrincipal user,
    AppDbContext dbContext) =>
{
    var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

    if (userIdClaim is null ||
        !int.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var conversationExists = await dbContext.Conversations
        .AsNoTracking()
        .AnyAsync(c =>
            c.Id == conversationId &&
            (c.CustomerId == userId ||
             c.PublisherId == userId));

    if (!conversationExists)
    {
        return Results.Problem(
            title: "Conversation not found",
            detail: "The conversation does not exist or you do not have access to it.",
            statusCode: StatusCodes.Status404NotFound
        );
    }

    var messages = await dbContext.Messages
        .AsNoTracking()
        .Where(m => m.ConversationId == conversationId)
        .OrderBy(m => m.SentAt)
        .Select(m => new MessageDto(
            m.Id,
            m.ConversationId,
            m.SenderId,
            m.Content,
            m.BookId,
            m.SentAt,
            m.IsRead
        ))
        .ToListAsync();

    return Results.Ok(messages);
});
        group.MapPost(
            "/{conversationId:int}/messages",
            async (
                int conversationId,
                CreateMessageDto request,
                ClaimsPrincipal user,
                AppDbContext dbContext,
                   IHubContext<ChatHub> hubContext) =>
            {
                var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

                if (userIdClaim is null ||
                    !int.TryParse(userIdClaim, out var userId))
                {
                    return Results.Unauthorized();
                }

                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return Results.Problem(
                        title: "Invalid message",
                        detail: "Message content cannot be empty.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                if (request.Content.Length > 4000)
                {
                    return Results.Problem(
                        title: "Invalid message",
                        detail: "Message content cannot exceed 4000 characters.",
                        statusCode: StatusCodes.Status400BadRequest
                    );
                }

                var conversation = await dbContext.Conversations
                    .FirstOrDefaultAsync(c =>
                        c.Id == conversationId &&
                        (c.CustomerId == userId ||
                         c.PublisherId == userId));

                if (conversation is null)
                {
                    return Results.Problem(
                        title: "Conversation not found",
                        detail: "The conversation does not exist or you do not have access to it.",
                        statusCode: StatusCodes.Status404NotFound
                    );
                }
                if (request.BookId.HasValue)
                {
                    var bookBelongsToPublisher = await dbContext.Books
                        .AsNoTracking()
                        .AnyAsync(b =>
                            b.Id == request.BookId.Value &&
                            b.CreatedByUserId == conversation.PublisherId);

                    if (!bookBelongsToPublisher)
                    {
                        return Results.Problem(
                            title: "Invalid book",
                            detail: "The selected book does not belong to the publisher of this conversation.",
                            statusCode: StatusCodes.Status400BadRequest
                        );
                    }
                }

                var message = new Message
                {
                    ConversationId = conversationId,
                    SenderId = userId,
                    Content = request.Content.Trim(),
                    BookId = request.BookId,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                dbContext.Messages.Add(message);

                conversation.LastMessageAt = message.SentAt;

                await dbContext.SaveChangesAsync();
                var receiverId =
                    conversation.CustomerId == userId
                        ? conversation.PublisherId
                        : conversation.CustomerId;

                var response = new MessageDto(
                    message.Id,
                    message.ConversationId,
                    message.SenderId,
                    message.Content,
                    message.BookId,
                    message.SentAt,
                    message.IsRead
                );
                await hubContext.Clients
                    .Group($"user-{receiverId}")
                    .SendAsync(
                        "ReceiveMessage",
                        response);
                return Results.Created(
                    $"/conversations/{conversationId}/messages/{message.Id}",
                    response
                );
            });
        group.MapPatch(
"/{conversationId:int}/messages/read",
async (
    int conversationId,
    ClaimsPrincipal user,
    AppDbContext dbContext) =>
{
    var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

    if (userIdClaim is null ||
        !int.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var conversationExists = await dbContext.Conversations
        .AsNoTracking()
        .AnyAsync(c =>
            c.Id == conversationId &&
            (c.CustomerId == userId ||
             c.PublisherId == userId));

    if (!conversationExists)
    {
        return Results.Problem(
            title: "Conversation not found",
            detail: "The conversation does not exist or you do not have access to it.",
            statusCode: StatusCodes.Status404NotFound
        );
    }

    var unreadMessages = await dbContext.Messages
        .Where(m =>
            m.ConversationId == conversationId &&
            m.SenderId != userId &&
            !m.IsRead)
        .ToListAsync();

    foreach (var message in unreadMessages)
    {
        message.IsRead = true;
    }

    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});
    }
}