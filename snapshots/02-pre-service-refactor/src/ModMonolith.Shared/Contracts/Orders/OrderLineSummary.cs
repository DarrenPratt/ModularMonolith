namespace ModMonolith.Shared.Contracts.Orders;

public sealed record OrderLineSummary(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
