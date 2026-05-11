using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModMonolith.Modules.Catalog.Application;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Contracts.Catalog;

namespace ModMonolith.Modules.Catalog;

public sealed class CatalogModule : IModule
{
    public string Name => "Catalog";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductCatalog, CatalogProductService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/catalog")
            .WithTags(Name);

        group.MapGet("/products", async (IProductCatalog productCatalog, CancellationToken cancellationToken) =>
        {
            var products = await productCatalog.GetAllAsync(cancellationToken);
            return Results.Ok(products);
        });

        group.MapPost("/products", async (CreateProductRequest request, IProductCatalog productCatalog, CancellationToken cancellationToken) =>
        {
            try
            {
                var product = await productCatalog.CreateAsync(
                    request.Name,
                    request.Price,
                    request.StockQuantity,
                    cancellationToken);

                return Results.Created($"/api/catalog/products/{product.Id}", product);
            }
            catch (InvalidOperationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["product"] = [exception.Message]
                });
            }
        });
    }

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var productCatalog = serviceProvider.GetRequiredService<IProductCatalog>();

        if ((await productCatalog.GetAllAsync(cancellationToken)).Count > 0)
        {
            return;
        }

        await productCatalog.CreateAsync("Modular Monolith Handbook", 49.00m, 25, cancellationToken);
        await productCatalog.CreateAsync("Architecture Diagram Poster", 19.00m, 40, cancellationToken);
        await productCatalog.CreateAsync("Integration Test Starter Kit", 99.00m, 10, cancellationToken);
    }
}
