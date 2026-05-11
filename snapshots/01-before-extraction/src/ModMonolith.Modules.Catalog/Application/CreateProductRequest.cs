namespace ModMonolith.Modules.Catalog.Application;

public sealed record CreateProductRequest(string Name, decimal Price, int StockQuantity);
