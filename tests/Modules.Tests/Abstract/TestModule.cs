using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Annular.Modules.Tests.Abstract;

internal sealed class TestModule : IModule
{
    public static int Count { get; private set; } = 0;

    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        Count++;
    }
}
