# Annular Modules

Trying to imitate [Angular Modules](https://blog.angular-university.io/angular2-ngmodule) in a way that makes sense for dotnet with IServiceCollection and IConfiguration.

Modules are registerd as `IOptions` and you can use the `IOptions` patterns to access them. By default the module is also registerd as a `Scoped` service and retrieves it's value from the `IOptionsMonitor.CurrentValue`.

> What remains to be added is maybe a special method with a configuration object instead of only IConfiguration that can be used from the module to decide what to register in the service provider.

> [!NOTE]  
> What remains to be done is a way to define different `options` for configuration (during add) versus options that will be registerd in DI as `IOptions`.

## Usage

### Simple
Create a class implementing the `IModule` interface:
```cs
public sealed class WeatherModule : IModule
{
    // This is called only once allowing us to register what we need
    // no matter if we are imported from another module.
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        // register services
        services.AddScoped<IWeatherService, InternalWeatherService>();
        // ...
    }
}
```

Register the module:
```cs
// In Program.cs
// this will execute the `Add(IServiceCollection, IConfiguration)` method This is executed only once.
builder.Services.AddModule<WeatherModule>(builder.Configuration);
```

### Validation
Implement IValidModule interface:
```cs
public sealed class WeatherModule : IModule, IValidModule
{
    public string ConnectionString {get; set;} = string.Empty;

    // This is called only once allowing us to register what we need
    // no matter if we are imported from another module.
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        // register services
        services.AddScoped<IWeatherService, InternalWeatherService>();
        services.AddDbContext<>();
        // ...
    }

    public ValidateOptionsResult Validate()
    {
        return string.IsNullOrWhiteSpace(ConnectionString)
            ? ValidateOptionsResult.Fail("Connection string is not valid")
            : ValidateOptionsResult.Success;
    }
}
```
DbContext implementation
```cs
public class WeatherDbContext : DbContext
{
    // Weather module is registerd as scoped
    public WeatherDbContext(WeatherModule module)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(module.ConnectionString);
        // You could even provide a callback here
        // allowing for arbitary databases, connection strings etc.
    }
}
```

### Advanced
Let's say we have a complex solution called `Broker` that is responsible to deliver all kinds of messages to our users using various ways like `Email`, `SMS` etc.

Our modules are:
- `Data` provides a way to talk to our database.
- `Publisher` provides ways to publish messages to a bus.
- `REST API` provides the REST API which internal services will use.
- `Dashboard` displays UI to see what is going on in our system database.
- `Consumer` provides ways to consume messages from a bus.

Module implementations:
```cs
public sealed class BrokerDataModule : IModule, IValidModule
{
    public string ConnectionString { get; set; } = string.Empty;

    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        // The project contains all Database Models etc and implements it in any way it
        // makes sense for it. When this is called all these services/handlers etc are registered.
    }

    public ValidateOptionsResult Validate()
    {
        return string.IsNullOrWhiteSpace(ConnectionString)
            ? ValidateOptionsResult.Fail("Connection string is not valid")
            : ValidateOptionsResult.Success;
    }
}

public sealed class BrokerPublisherModule : IModule
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModule<BrokerDataModule>(configuration);
        // we could also only focus on publishing and the consumer could focus on saving to the db
        // here we assume we want to save some details to the database before publishing to the bus.
    }
}

public sealed class BrokerRestApiModule : IModule
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        // Register any services like caching, authorization policies etc.

        // Here we register the Data module since we will use the services to access the database data.
        services.AddModule<BrokerDataModule>(configuration);

        // Our API will provide endpoints to publish messages to internal services so we need the publishing services.
        services.AddModule<BrokerPublisherModule>(configuration);
    }
}

public sealed class BrokerDashboardModule : IModule
{
    public string ClientId { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        // Register dashboard services like HttpClients, and OAuth.
        // this one assumes the dashboard is a SPA application served statically
        // on which we will provide the ClientId and ApiUrl since it might be server from anywhere.
    }
}

public sealed class BrokerConsumerModule : IModule
{
    public static void Add(IServiceCollection services, IConfiguration configuration)
    {
        // Register services needed so that you can consume messages from the bus.

        // Register the data module so that you can access and update the database.
        services.AddModule<BrokerDataModule>(configuration);
    }
}
```

This way all modules will register once while those that have validations will throw and make users provide their options. Since `Configure` is used internally you can also bind all of the modules to certain sections. This demonstrates that modules can use other modules.
