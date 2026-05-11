using System.Reflection;

namespace ModMonolith.Shared.Abstractions;

public sealed class ModuleRegistry
{
    public ModuleRegistry(IEnumerable<IModule> modules)
    {
        Modules = modules.ToArray();
        Assemblies = Modules
            .Select(module => module.GetType().Assembly)
            .Distinct()
            .ToArray();
    }

    public IReadOnlyList<IModule> Modules { get; }

    public IReadOnlyCollection<Assembly> Assemblies { get; }
}
