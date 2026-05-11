namespace ModMonolith.Shared.Contracts.Orders;

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummary>> GetAllAsync(CancellationToken cancellationToken);

    Task<OrderSummary> CreateAsync(CreateOrder request, CancellationToken cancellationToken);
}
