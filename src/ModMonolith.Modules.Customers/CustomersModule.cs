using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModMonolith.Modules.Customers.Application;
using ModMonolith.Modules.Customers.Domain;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Contracts.Customers;
using ModMonolith.Shared.Persistence;

namespace ModMonolith.Modules.Customers;

public sealed class CustomersModule : IModule
{
    public string Name => "Customers";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICustomerDirectory, CustomerDirectoryService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/customers")
            .WithTags(Name);

        group.MapGet("/customers", async (ICustomerDirectory customerDirectory, CancellationToken cancellationToken) =>
        {
            var customers = await customerDirectory.GetAllAsync(cancellationToken);
            return Results.Ok(customers);
        });

        group.MapPost("/customers", async (CreateCustomerRequest request, ICustomerDirectory customerDirectory, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Name is required."]
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["Email is required."]
                });
            }

            try
            {
                var customer = await customerDirectory.CreateAsync(request.Name, request.Email, cancellationToken);
                return Results.Created($"/api/customers/customers/{customer.Id}", customer);
            }
            catch (InvalidOperationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = [exception.Message]
                });
            }
        });
    }

    public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<ModMonolithDbContext>();

        if (await dbContext.Set<Customer>().AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.AddRange(
            new Customer
            {
                Id = Guid.NewGuid(),
                Name = "Ava Bennett",
                Email = "ava.bennett@example.com",
                CreatedUtc = DateTime.UtcNow.AddDays(-20)
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                Name = "Marcus Doyle",
                Email = "marcus.doyle@example.com",
                CreatedUtc = DateTime.UtcNow.AddDays(-12)
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                Name = "Priya Shah",
                Email = "priya.shah@example.com",
                CreatedUtc = DateTime.UtcNow.AddDays(-4)
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
