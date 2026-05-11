namespace ModMonolith.Shared.Contracts.Orders;

public sealed record CreateOrderLine(Guid ProductId, int Quantity);
