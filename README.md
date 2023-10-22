# Super Simple Dependency Injection

This is a very simple DI framework targeted at Console apps and games. The DI Container does not need to be "built". This allows your app to be extendable with DLLs that may be loaded later in the applications lifecycle. 

## How to use
Some use examples.

```cs
// in this example we will have a IConfiguration 
var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appconfig.json", false, true);
var container = new DependencyInjectionContainer();

// Configure can be called as many times as you want on a container at any point in the applications lifecycle.
container.Configure(c =>
{
    _ = c.ExportInstance(configuration).As<IConfiguration>(); // We can load the instance, note that the Lifecycle type on Instances are singletons.
    _ = builder.Export<TCPServer>(); // Transient is the default Lifecycle for any object added to the container without a specificed lifecycle. Transient creates a new instance every time its located.
    _ = builder.Export<PacketRouter>().Lifestyle.Singleton(); // Singleton to have only one instance. It is not created until its located. SSDI only supported Singleton and Transient.
    _ = builder.Export<ClientServer>().WithCtorParam(PacketScope.ClientToAuth).Lifestyle.Singleton(); // WithCtorParam allows you to specify a parammeter of the constructor. This has several overloads. First is by Type
    _ = builder.Export<ClientServer>().WithCtorParam("scope", PacketScope.ClientToAuth).Lifestyle.Singleton(); // Second is by parameter name. This will match with the name of the parameter in your constructor.
    _ = builder.Export<ClientServer>().WithCtorParam(0, PacketScope.ClientToAuth).Lifestyle.Singleton(); // Third is by position of the parameter in the constructor. 0 based. 
    _ = builder.Export<ClientServer>().WithCtorPositionalParams(PacketScope.ClientToAuth, 1234, "MyConn").Lifestyle.Singleton(); // Fourth is by specifying a number of parames. These are assumed to be starting with position 0. Any parameters not provided will attempted to be located in the container.

    // Add alias for a shared interface. NOTE: SSDI does not look at what interfaces a object is implimenting. If you plan to locate by interface later you need to register the alias.
    _ = builder.Export<AuthPacketServer>().As<IPacketRouter>();
    _ = builder.Export<HomePacketServer>().As<IPacketRouter>();
    _ = builder.Export<ShopPacketServer>().As<IPacketRouter>();
});

// Any parameters not provided will attempted to be located in the container.
var server = container.Locate<ClientServer>(); // simple locate.
var routes = container.Locate<IEnumerable<IPacketRouter>>(); // locate all instances of interface.
var tcp = container.Locate<TCPServer>(0, "127.0.0.1") // with one positional parameter
tcp = container.LocateWithTypedParams<TCPServer>("127.0.0.1", 8080) // with typed parameters
tcp = container.LocateWithPositionalParams<TCPServer>("127.0.0.1", 8080) // with positional parameters starting at position 0.
tcp = container.LocateWithNamedParameters<TCPServer>(("address", "127.0.0.1"), ("port", 8080)) // with positional parameters starting at position 0.
```

