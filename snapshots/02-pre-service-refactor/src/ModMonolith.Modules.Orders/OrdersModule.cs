using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModMonolith.Modules.Orders.Application;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Contracts.Catalog;
using ModMonolith.Shared.Contracts.Customers;
using ModMonolith.Shared.Contracts.Orders;

namespace ModMonolith.Modules.Orders;

public sealed class OrdersModule : IModule
{
    public string Name => "Orders";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOrderService, OrderService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders")
            .WithTags(Name);

        group.MapGet("/orders", async (IOrderService orderService, CancellationToken cancellationToken) =>
        {
            var orders = await orderService.GetAllAsync(cancellationToken);
            return Results.Ok(orders);
        });

        group.MapPost("/orders", async (
            CreateOrderRequest request,
            IOrderService orderService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var order = await orderService.CreateAsync(
                    new CreateOrder(
                        request.CustomerId,
                        request.Lines.Select(line => new CreateOrderLine(line.ProductId, line.Quantity)).ToArray()),
                    cancellationToken);

                return Results.Created($"/api/orders/orders/{order.Id}", order);
            }
            catch (InvalidOperationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["order"] = [exception.Message]
                });
            }
        });
    }

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var orderService = serviceProvider.GetRequiredService<IOrderService>();

        if ((await orderService.GetAllAsync(cancellationToken)).Count > 0)
        {
            return;
        }

        var productCatalog = serviceProvider.GetRequiredService<IProductCatalog>();
        var customerDirectory = serviceProvider.GetRequiredService<ICustomerDirectory>();
        var products = (await productCatalog.GetAllAsync(cancellationToken))
            .Take(2)
            .ToArray();
        var customer = (await customerDirectory.GetAllAsync(cancellationToken)).FirstOrDefault();

        if (products.Length < 2 || customer is null)
        {
            return;
        }

        await orderService.CreateAsync(
            new CreateOrder(
                customer.Id,
                products.Select(product => new CreateOrderLine(product.Id, 1)).ToArray()),
            cancellationToken);
    }
}
