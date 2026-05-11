namespace ModMonolith.Shared.Contracts.Orders;

public sealed record CreateOrder(Guid CustomerId, IReadOnlyList<CreateOrderLine> Lines);
