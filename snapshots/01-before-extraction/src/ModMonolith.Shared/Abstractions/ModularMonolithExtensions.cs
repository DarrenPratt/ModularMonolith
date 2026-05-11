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

    public static async Task SeedModulesAsync(
        this WebApplication app,
        bool recreateOnSchemaMismatch = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await SeedOnceAsync(app, cancellationToken);
        }
        catch (Exception exception) when (recreateOnSchemaMismatch && IsSchemaMismatch(exception))
        {
            await using var recoveryScope = app.Services.CreateAsyncScope();
            var recoveryContext = recoveryScope.ServiceProvider.GetRequiredService<ModMonolithDbContext>();

            await recoveryContext.Database.EnsureDeletedAsync(cancellationToken);
            await recoveryContext.Database.EnsureCreatedAsync(cancellationToken);

            await SeedOnceAsync(app, cancellationToken);
        }
    }

    private static async Task SeedOnceAsync(WebApplication app, CancellationToken cancellationToken)
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

    private static bool IsSchemaMismatch(Exception exception)
    {
        return exception.ToString().Contains("Invalid object name", StringComparison.OrdinalIgnoreCase);
    }
}
