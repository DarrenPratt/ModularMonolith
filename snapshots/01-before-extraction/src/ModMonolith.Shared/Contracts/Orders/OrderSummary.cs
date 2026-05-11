namespace ModMonolith.Shared.Contracts.Orders;

public sealed record OrderSummary(
    Guid Id,
    string Number,
    Guid CustomerId,
    string CustomerName,
    DateTime CreatedUtc,
    decimal TotalAmount,
    IReadOnlyList<OrderLineSummary> Lines);
