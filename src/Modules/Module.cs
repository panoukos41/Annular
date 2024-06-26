using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Annular.Modules;

/// <summary>
/// Extension methods to register modules.
/// </summary>
public static class Module
{
    private static readonly Type validType = typeof(IValidModule);

    /// <summary>
    /// Add a module to the service collection.
    /// </summary>
    /// <typeparam name="TModule">The module to register.</typeparam>
    /// <param name="services">The service collection to register at.</param>
    /// <param name="configuration">Configuration to pass to the module Add method.</param>
    /// <param name="configure">Options configure callback.</param>
    /// <returns>The service collection for further registrations.</returns>
    public static IServiceCollection AddModule<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TModule>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<TModule>? configure = null
    )
        where TModule : class, IModule, new()
    {
        var moduleType = typeof(TModule);
        var descriptor = ServiceDescriptor.Scoped(Factory<TModule>);
        if (!services.Contains(descriptor, ServiceDescriptorComparer.Instance))
        {
            services.Add(descriptor);
            if (moduleType.IsAssignableTo(validType))
            {
                services.AddSingleton<IValidateOptions<TModule>, ValidateModule<TModule>>();
            }
            TModule.Add(services, configuration);
        }
        if (configure is { })
        {
            services.Configure(configure);
        }
        return services;
    }

    private static TModule Factory<TModule>(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<IOptionsMonitor<TModule>>().CurrentValue;
    }

    private sealed class ValidateModule<TModule> : IValidateOptions<TModule>
        where TModule : class, IModule, new()
    {
        public ValidateOptionsResult Validate(string? name, TModule module)
        {
            return module is IValidModule validModule ? validModule.Validate() : ValidateOptionsResult.Success;
        }
    }

    private class ServiceDescriptorComparer : IEqualityComparer<ServiceDescriptor>
    {
        public static ServiceDescriptorComparer Instance { get; } = new();

        public bool Equals(ServiceDescriptor? x, ServiceDescriptor? y)
        {
            return x?.ServiceType.Equals(y?.ServiceType) ?? false;
        }

        public int GetHashCode([DisallowNull] ServiceDescriptor obj)
        {
            return obj.ServiceType.GetHashCode();
        }
    }
}
