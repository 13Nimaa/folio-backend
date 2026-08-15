public record CreateOrderDto(
    List<CreateOrderItemDto> Items
);

public record CreateOrderItemDto(
    int BookId,
    int Quantity
);