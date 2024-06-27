using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Annular.Modules;

/// <summary>
/// Defines a module. Modules contain all the services
/// and other modules needed for the module to function correctly.
/// eg: The WeatherModule should register it's WeatherService and WeatherStore implementations
/// for use in the application later. Modules can also act as options for runtime behavior.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Method to add module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The provided IConfiguration.</param>
    abstract static void Add(IServiceCollection services, IConfiguration configuration);
}
