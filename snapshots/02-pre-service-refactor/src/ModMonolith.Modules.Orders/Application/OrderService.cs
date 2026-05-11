using Microsoft.EntityFrameworkCore;
using ModMonolith.Modules.Orders.Domain;
using ModMonolith.Shared.Contracts.Catalog;
using ModMonolith.Shared.Contracts.Customers;
using ModMonolith.Shared.Contracts.Orders;
using ModMonolith.Shared.Persistence;

namespace ModMonolith.Modules.Orders.Application;

public sealed class OrderService(
    ModMonolithDbContext dbContext,
    IProductCatalog productCatalog,
    ICustomerDirectory customerDirectory) : IOrderService
{
    public async Task<IReadOnlyList<OrderSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<Order>()
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedUtc)
            .Select(order => new OrderSummary(
                order.Id,
                order.Number,
                order.CustomerId,
                order.CustomerName,
                order.CreatedUtc,
                order.TotalAmount,
                order.Lines.Select(line => new OrderLineSummary(
                    line.ProductId,
                    line.ProductName,
                    line.Quantity,
                    line.UnitPrice)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderSummary> CreateAsync(CreateOrder request, CancellationToken cancellationToken)
    {
        var customer = await customerDirectory.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException("The selected customer was not found.");
        }

        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("At least one order line is required.");
        }

        if (request.Lines.Any(line => line.Quantity <= 0))
        {
            throw new InvalidOperationException("Each order line must have a quantity greater than zero.");
        }

        var products = await productCatalog.GetByIdsAsync(
            request.Lines.Select(line => line.ProductId),
            cancellationToken);

        var productMap = products.ToDictionary(product => product.Id);
        var missingIds = request.Lines
            .Select(line => line.ProductId)
            .Distinct()
            .Where(productId => !productMap.ContainsKey(productId))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new InvalidOperationException($"Products not found: {string.Join(", ", missingIds)}");
        }

        var insufficientStock = request.Lines
            .Where(line => productMap[line.ProductId].StockOnHand < line.Quantity)
            .Select(line => $"{productMap[line.ProductId].Name} only has {productMap[line.ProductId].StockOnHand} units available.")
            .ToArray();

        if (insufficientStock.Length > 0)
        {
            throw new InvalidOperationException(string.Join(" ", insufficientStock));
        }

        await productCatalog.ReserveStockAsync(
            request.Lines
                .Select(line => new ProductReservation(line.ProductId, line.Quantity))
                .ToArray(),
            cancellationToken);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Number = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CreatedUtc = DateTime.UtcNow
        };

        foreach (var line in request.Lines)
        {
            var product = productMap[line.ProductId];
            order.Lines.Add(new OrderLine
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = line.ProductId,
                ProductName = product.Name,
                Quantity = line.Quantity,
                UnitPrice = product.Price
            });
        }

        order.TotalAmount = order.Lines.Sum(line => line.Quantity * line.UnitPrice);

        dbContext.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrderSummary(
            order.Id,
            order.Number,
            order.CustomerId,
            order.CustomerName,
            order.CreatedUtc,
            order.TotalAmount,
            order.Lines.Select(line => new OrderLineSummary(
                line.ProductId,
                line.ProductName,
                line.Quantity,
                line.UnitPrice)).ToList());
    }
}
