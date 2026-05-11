namespace ModMonolith.Shared.Contracts.Catalog;

public interface IProductCatalog
{
    Task<IReadOnlyList<ProductSummary>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductSummary>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task ReserveStockAsync(IReadOnlyCollection<ProductReservation> reservations, CancellationToken cancellationToken);
}
