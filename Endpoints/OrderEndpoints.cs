using System.Security.Claims;
using BooksProject.Authentication;
using BooksProject.Data;
using Microsoft.EntityFrameworkCore;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateOrderDto dto,
            ClaimsPrincipal user,
            AppDbContext dbcontext) =>
        {
            var userIdClaim = user.FindFirstValue(TokenService.SubClaim);
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();

            }
            if (dto.Items.Count == 0)
            {
                return Results.BadRequest("Order must contain at least one item.");

            }
            if (dto.Items.Any(x => x.Quantity <= 0))
            {
                return Results.BadRequest("Quantity must be greater than zero.");
            }
            if (dto.Items
    .GroupBy(x => x.BookId)
    .Any(g => g.Count() > 1))
            {
                return Results.BadRequest("An order cannot contain the same book multiple times.");
            }
            var bookIds = dto.Items
            .Select(x => x.BookId)
            .ToList();
            var books = await dbcontext.Books.Where(b => bookIds.Contains(b.Id)).ToListAsync();

            if (books.Count != bookIds.Count)
            {
                return Results.BadRequest("One or more books were not found.");
            }
            decimal totalPrice = 0;
            foreach (var item in dto.Items)
            {
                var book = books.First(x => x.Id == item.BookId);
                totalPrice += book.Price * item.Quantity;

            }
            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Pending,
                TotalPrice = totalPrice



            };
            foreach (var item in dto.Items)
            {
                var book = books.First(x => x.Id == item.BookId);
                order.Items.Add(new OrderItem
                {
                    BookId = book.Id,
                    Quantity = item.Quantity,
                    UnitPrice = book.Price
                });
            }
            dbcontext.Orders.Add(order);

            await dbcontext.SaveChangesAsync();
            return Results.Created(
                $"/api/orders/{order.Id}",
                new
                {
                    order.Id,
                    order.Status,
                    order.TotalPrice,
                    order.CreatedAt
                });

        });
        app.MapGet("/", async (ClaimsPrincipal user, AppDbContext dbContext) =>
        {
            var userIdClaim = user.FindFirstValue(TokenService.SubClaim);
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }
            var orders = await dbContext.Orders.AsNoTracking().
            Where(o => o.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
                   .Select(x => new OrderDto(
            x.Id,
            x.Status,
            x.TotalPrice,
            x.CreatedAt
        ))
        .ToListAsync();

            return Results.Ok(orders);

        });
        app.MapGet("/{id:int}", async (
            ClaimsPrincipal user,
            int OrderId,
            AppDbContext dbContext
        ) =>
        {
            var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }
            var order = await dbContext.Orders
                .AsNoTracking()
                .Where(x => x.Id == OrderId && x.UserId == userId)
                .Select(x => new OrderDetailsDto(
                    x.Id,
                    x.Status,
                    x.TotalPrice,
                    x.CreatedAt,
                    x.Items.Select(item => new OrderItemDto(
                        item.BookId,
                        item.Book.Title,
                        item.Quantity,
                        item.UnitPrice
                    )).ToList()
                ))
                .FirstOrDefaultAsync();

            if (order is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(order);
        });
group.MapPatch("/{id:int}/cancel", async (
    int id,
    ClaimsPrincipal user,
    AppDbContext dbContext) =>
{
    var userIdClaim = user.FindFirstValue(TokenService.SubClaim);

    if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var order = await dbContext.Orders
        .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

    if (order is null)
    {
        return Results.NotFound();
    }

    if (order.Status != OrderStatus.Pending)
    {
        return Results.BadRequest(
            "Only pending orders can be cancelled.");
    }

    order.Status = OrderStatus.Cancelled;

    await dbContext.SaveChangesAsync();

    return Results.Ok(new
    {
        order.Id,
        order.Status
    });
});
    }

}
