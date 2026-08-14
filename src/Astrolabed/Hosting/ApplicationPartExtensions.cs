using System.Reflection;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Hosting;

public static class ApplicationPartExtensions
{
    /// <summary>
    /// Scans the assembly containing <typeparamref name="TMarker"/> and adds application parts
    /// strictly for controller classes located within the specified namespace.
    /// </summary>
    /// <typeparamref name="TMarker">A type located inside the target assembly.</typeparamref>
    /// <param name="builder">The IMvcBuilder instance.</param>
    /// <param name="targetNamespace">The target namespace to scan for controllers (e.g. "Astrolabed.Api.Controllers").</param>
    public static IMvcBuilder AddControllersFromNamespace<TMarker>(
        this IMvcBuilder builder,
        string targetNamespace)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetNamespace);

        var assembly = typeof(TMarker).Assembly;

        // 1. Add the assembly part so ASP.NET Core registers the assembly
        builder.AddApplicationPart(assembly);

        // 2. Configure a custom feature provider to filter controllers by namespace using reflection
        builder.ConfigureApplicationPartManager(manager =>
        {
            manager.FeatureProviders.Add(new NamespaceControllerFeatureProvider(assembly, targetNamespace));
        });

        return builder;
    }
}

/// <summary>
/// Custom ControllerFeatureProvider that filters candidates by assembly and target namespace using reflection.
/// </summary>
internal sealed class NamespaceControllerFeatureProvider : ControllerFeatureProvider
{
    private readonly Assembly _targetAssembly;
    private readonly string _targetNamespace;

    public NamespaceControllerFeatureProvider(Assembly targetAssembly, string targetNamespace)
    {
        _targetAssembly = targetAssembly;
        _targetNamespace = targetNamespace;
    }

    protected override bool IsController(TypeInfo typeInfo)
    {
        // Must pass default MVC controller checks first (inherits ControllerBase/Controller or has [ApiController]/[Controller])
        if (!base.IsController(typeInfo))
        {
            return false;
        }

        // Use reflection to enforce assembly and namespace matching
        return typeInfo.Assembly == _targetAssembly
            && typeInfo.Namespace is not null
            && (typeInfo.Namespace == _targetNamespace || typeInfo.Namespace.StartsWith($"{_targetNamespace}.", StringComparison.Ordinal));
    }
}
