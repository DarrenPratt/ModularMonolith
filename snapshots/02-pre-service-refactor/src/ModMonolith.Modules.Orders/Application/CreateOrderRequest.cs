namespace ModMonolith.Modules.Orders.Application;

public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(Guid ProductId, int Quantity);
