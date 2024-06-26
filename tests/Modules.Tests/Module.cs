using Annular.Modules.Tests.Abstract;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Annular.Modules.Tests;

public class Module
{
    private readonly ServiceCollection services;
    private readonly ConfigurationManager configuration;
    private readonly IConfigurationBuilder configurationBuilder;

    public Module()
    {
        services = new();
        configuration = new();
        configurationBuilder = configuration;
    }

    [Fact]
    public void Should_Add_Only_Once()
    {
        var iterations = 10_000;
        var configureCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            services.AddModule<TestModule>(configuration, c => Interlocked.Increment(ref configureCount));
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<TestModule>().Should().NotBeNull();

        configureCount.Should().Be(iterations);
        TestModule.Count.Should().Be(1);
    }

    [Fact]
    public void Should_Validate_When_Changed()
    {
        var iterations = 10_000;
        var configureCount = 0;

        var configurationProvider = new TestConfigurationProvider();
        configurationBuilder.Add(configurationProvider);

        services.Configure<TestModuleValid>(configuration.GetSection(nameof(TestModuleValid)));

        for (int i = 0; i < iterations; i++)
        {
            services.AddModule<TestModuleValid>(configuration, c => Interlocked.Increment(ref configureCount));
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<TestModuleValid>().Should().NotBeNull();

        configureCount.Should().Be(iterations);
        TestModuleValid.Count.Should().Be(1);
        TestModuleValid.ValidateCount.Should().Be(1);

        configurationProvider.Reload();


        configureCount.Should().Be(iterations * 2);
        TestModuleValid.Count.Should().Be(1);
        TestModuleValid.ValidateCount.Should().Be(2);
    }
}
