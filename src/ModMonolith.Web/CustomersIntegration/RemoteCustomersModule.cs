using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModMonolith.Shared.Abstractions;
using ModMonolith.Shared.Contracts.Customers;

namespace ModMonolith.Web.CustomersIntegration;

public sealed class RemoteCustomersModule : IModule
{
    public string Name => "Customers API";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Services:Customers:BaseUrl"] ?? "http://localhost:5241";

        services.AddHttpClient<ICustomerDirectory, CustomerDirectoryHttpClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        });
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    public Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
