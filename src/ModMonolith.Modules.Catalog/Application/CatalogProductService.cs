using Microsoft.EntityFrameworkCore;
using ModMonolith.Modules.Catalog.Domain;
using ModMonolith.Shared.Contracts.Catalog;
using ModMonolith.Shared.Persistence;

namespace ModMonolith.Modules.Catalog.Application;

public sealed class CatalogProductService(ModMonolithDbContext dbContext) : IProductCatalog
{
    public async Task<IReadOnlyList<ProductSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<Product>()
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new ProductSummary(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSummary>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var productIds = ids.Distinct().ToArray();

        return await dbContext.Set<Product>()
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new ProductSummary(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity))
            .ToListAsync(cancellationToken);
    }

    public async Task ReserveStockAsync(
        IReadOnlyCollection<ProductReservation> reservations,
        CancellationToken cancellationToken)
    {
        var requestedQuantities = reservations
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var products = await dbContext.Set<Product>()
            .Where(product => requestedQuantities.Keys.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var (productId, quantity) in requestedQuantities)
        {
            if (!products.TryGetValue(productId, out var product))
            {
                throw new InvalidOperationException($"Product '{productId}' was not found.");
            }

            if (product.StockQuantity < quantity)
            {
                throw new InvalidOperationException(
                    $"Product '{product.Name}' does not have enough stock. Requested {quantity}, available {product.StockQuantity}.");
            }

            product.StockQuantity -= quantity;
        }
    }
}
