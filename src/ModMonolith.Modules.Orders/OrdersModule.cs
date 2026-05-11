using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModMonolith.Modules.Orders.Application;
using ModMonolith.Modules.Orders.Domain;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Contracts.Catalog;
using ModMonolith.Shared.Persistence;

namespace ModMonolith.Modules.Orders;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders")
            .WithTags(Name);

        group.MapGet("/orders", async (ModMonolithDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var orders = await dbContext.Set<Order>()
                .AsNoTracking()
                .OrderByDescending(order => order.CreatedUtc)
                .Select(order => new
                {
                    order.Id,
                    order.Number,
                    order.CreatedUtc,
                    order.TotalAmount,
                    Lines = order.Lines.Select(line => new
                    {
                        line.ProductId,
                        line.ProductName,
                        line.Quantity,
                        line.UnitPrice
                    })
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(orders);
        });

        group.MapPost("/orders", async (
            CreateOrderRequest request,
            IProductCatalog productCatalog,
            ModMonolithDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (request.Lines.Count == 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = ["At least one order line is required."]
                });
            }

            if (request.Lines.Any(line => line.Quantity <= 0))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = ["Each order line must have a quantity greater than zero."]
                });
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
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = [$"Products not found: {string.Join(", ", missingIds)}"]
                });
            }

            var insufficientStock = request.Lines
                .Where(line => productMap[line.ProductId].StockOnHand < line.Quantity)
                .Select(line => $"{productMap[line.ProductId].Name} only has {productMap[line.ProductId].StockOnHand} units available.")
                .ToArray();

            if (insufficientStock.Length > 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = insufficientStock
                });
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

            return Results.Created($"/api/orders/orders/{order.Id}", new
            {
                order.Id,
                order.Number,
                order.CreatedUtc,
                order.TotalAmount
            });
        });
    }

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<ModMonolithDbContext>();

        if (await dbContext.Set<Order>().AnyAsync(cancellationToken))
        {
            return;
        }

        var productCatalog = serviceProvider.GetRequiredService<IProductCatalog>();
        var products = (await productCatalog.GetAllAsync(cancellationToken))
            .Take(2)
            .ToArray();

        if (products.Length < 2)
        {
            return;
        }

        await productCatalog.ReserveStockAsync(
            products.Select(product => new ProductReservation(product.Id, 1)).ToArray(),
            cancellationToken);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Number = "ORD-SEED-0001",
            CreatedUtc = DateTime.UtcNow,
            TotalAmount = products.Sum(product => product.Price),
            Lines = products.Select(product => new OrderLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 1,
                UnitPrice = product.Price
            }).ToList()
        };

        foreach (var line in order.Lines)
        {
            line.OrderId = order.Id;
        }

        dbContext.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
