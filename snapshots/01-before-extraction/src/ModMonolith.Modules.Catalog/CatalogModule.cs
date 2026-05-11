using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModMonolith.Modules.Catalog.Application;
using ModMonolith.Modules.Catalog.Domain;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Contracts.Catalog;
using ModMonolith.Shared.Persistence;

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

        group.MapGet("/products", async (ModMonolithDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var products = await dbContext.Set<Product>()
                .AsNoTracking()
                .OrderBy(product => product.Name)
                .Select(product => new ProductSummary(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.StockQuantity))
                .ToListAsync(cancellationToken);

            return Results.Ok(products);
        });

        group.MapPost("/products", async (CreateProductRequest request, ModMonolithDbContext dbContext, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Name is required."]
                });
            }

            if (request.Price <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["price"] = ["Price must be greater than zero."]
                });
            }

            if (request.StockQuantity < 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["stockQuantity"] = ["Stock quantity cannot be negative."]
                });
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };

            dbContext.Add(product);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/catalog/products/{product.Id}", new ProductSummary(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity));
        });
    }

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<ModMonolithDbContext>();

        if (await dbContext.Set<Product>().AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.AddRange(
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Modular Monolith Handbook",
                Price = 49.00m,
                StockQuantity = 25
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Architecture Diagram Poster",
                Price = 19.00m,
                StockQuantity = 40
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Integration Test Starter Kit",
                Price = 99.00m,
                StockQuantity = 10
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
