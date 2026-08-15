public record OrderDto(
    int Id,
    OrderStatus Status,
    decimal TotalPrice,
    DateTime CreatedAt
);