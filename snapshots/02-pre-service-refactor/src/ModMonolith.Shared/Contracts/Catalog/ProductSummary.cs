namespace ModMonolith.Shared.Contracts.Catalog;

public sealed record ProductSummary(Guid Id, string Name, decimal Price, int StockOnHand);
