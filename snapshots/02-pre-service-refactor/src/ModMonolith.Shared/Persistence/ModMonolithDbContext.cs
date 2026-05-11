using Microsoft.EntityFrameworkCore;
using ModMonolith.Shared.Abstractions;

namespace ModMonolith.Shared.Persistence;

public sealed class ModMonolithDbContext : DbContext
{
    private readonly ModuleRegistry _moduleRegistry;

    public ModMonolithDbContext(
        DbContextOptions<ModMonolithDbContext> options,
        ModuleRegistry moduleRegistry) : base(options)
    {
        _moduleRegistry = moduleRegistry;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var assembly in _moduleRegistry.Assemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        }

        base.OnModelCreating(modelBuilder);
    }
}
