using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModMonolith.Shared.Persistence;

namespace ModMonolith.Shared.Abstractions;

public static class ModularMonolithExtensions
{
    public static IServiceCollection AddModularMonolith(
        this IServiceCollection services,
        IConfiguration configuration,
        params IModule[] modules)
    {
        var registry = new ModuleRegistry(modules);
        services.AddSingleton(registry);

        foreach (var module in registry.Modules)
        {
            module.RegisterServices(services, configuration);
        }

        return services;
    }

    public static WebApplication MapModules(this WebApplication app)
    {
        var registry = app.Services.GetRequiredService<ModuleRegistry>();

        foreach (var module in registry.Modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }

    public static async Task SeedModulesAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var registry = services.GetRequiredService<ModuleRegistry>();
        var dbContext = services.GetRequiredService<ModMonolithDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        foreach (var module in registry.Modules)
        {
            await module.SeedAsync(services, cancellationToken);
        }
    }
}
