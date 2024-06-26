using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Annular.Modules.Tests.Abstract;

internal sealed class TestModuleValid : IModule, IValidModule
{
    public static int Count { get; private set; } = 0;

    public static int ValidateCount { get; private set; } = 0;

    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        Count++;
    }

    public ValidateOptionsResult Validate()
    {
        ValidateCount++;
        return ValidateOptionsResult.Success;
    }
}
