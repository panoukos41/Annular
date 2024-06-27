using Microsoft.Extensions.Configuration;

namespace Annular.Modules.Tests.Abstract;

internal sealed class TestConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    public void Reload() => OnReload();

    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
}
