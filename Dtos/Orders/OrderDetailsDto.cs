public record OrderDetailsDto(
    int Id,
    OrderStatus Status,
    decimal TotalPrice,
    DateTime CreatedAt,
    List<OrderItemDto> Items
);

public record OrderItemDto(
    int BookId,
    string Title,
    int Quantity,
    decimal UnitPrice
);