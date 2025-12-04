# Super Simple Dependency Injection (SSDI)

[![NuGet](https://img.shields.io/nuget/v/SSDI.svg)](https://www.nuget.org/packages/SSDI/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, high-performance dependency injection framework for .NET designed for games and console applications. **No container build step required** — register types at any point during your application's lifecycle.

## Features

- **No build step** — Register types dynamically at runtime
- **Fast resolution** — Optimized for game loops and high-frequency calls
- **Scoped lifetime** — Per-player, per-request, or per-session instances
- **Unregister support** — Hot-swap implementations at runtime
- **Lightweight** — Minimal allocations, ideal for games

## Quick Start

```cs
var container = new DependencyInjectionContainer();

container.Configure(c =>
{
    c.Export<MyService>();
    c.Export<MyRepository>().Lifestyle.Singleton();
    c.Export<PlayerData>().As<IPlayerData>().Lifestyle.Scoped();
});

var service = container.Locate<MyService>();
```

## Lifestyles

| Lifestyle | Description |
|-----------|-------------|
| **Transient** (default) | New instance every resolution |
| **Singleton** | One instance for app lifetime |
| **Scoped** | One instance per scope |

```cs
container.Configure(c =>
{
    c.Export<GameEngine>().Lifestyle.Singleton();
    c.Export<Enemy>();  // Transient by default
    c.Export<PlayerInventory>().Lifestyle.Scoped();
});
```

## Scoped Services

```cs
// Create a scope (e.g., per player)
using var playerScope = container.CreateScope();

var inventory1 = playerScope.Locate<IInventory>();
var inventory2 = playerScope.Locate<IInventory>();
// Same instance within scope

// All scoped IDisposable services disposed when scope ends
```

## Interface Registration

SSDI requires explicit interface registration:

```cs
container.Configure(c =>
{
    c.Export<SqlRepository>().As<IRepository>();

    // Multiple implementations
    c.Export<AuthHandler>().As<IPacketHandler>();
    c.Export<GameHandler>().As<IPacketHandler>();
});

// Resolve all implementations
var handlers = container.Locate<IEnumerable<IPacketHandler>>();
```

## Constructor Parameters

```cs
container.Configure(c =>
{
    // By type
    c.Export<GameServer>().WithCtorParam<int>(8080);

    // By name
    c.Export<GameServer>().WithCtorParam("port", 8080);

    // By position
    c.Export<GameServer>().WithCtorParam(0, "MyServer");

    // Multiple positional
    c.Export<GameServer>().WithCtorPositionalParams("MyServer", 8080, true);
});
```

## Combined Registration and Runtime Parameters

Register some parameters at configuration time, provide others at resolution:

```cs
// Register host at configuration time
container.Configure(c =>
    c.Export<GameServer>()
        .WithCtorParam(0, "game.server.com")
        .WithCtorParam(1, 443));

// Provide remaining parameter at runtime
var server = container.Locate<GameServer>(
    DIParameter.Positional(2, true)); // useSsl
```

## Unregistered Event

Subscribe to notifications when services are unregistered:

```cs
container.Unregistered += (sender, args) =>
{
    Console.WriteLine($"Unregistered: {args.UnregisteredType.Name}");
    if (args.WasDisposed)
        Console.WriteLine("  Instance was disposed");
};
```

## Unregistering Services

```cs
// Unregister a type (disposes singleton if IDisposable)
container.Unregister<OldService>();

// Unregister all implementations of an interface
container.UnregisterAll<IPacketHandler>();

// Check registration
if (container.IsRegistered<ILogger>()) { ... }
```

## Pre-built Instances

```cs
var config = LoadConfiguration();
container.Configure(c =>
{
    c.ExportInstance(config).As<IConfiguration>();
});
```

## Dynamic Registration

Perfect for plugin systems and runtime extensibility:

```cs
// Load plugin assembly
var assembly = Assembly.LoadFrom(pluginPath);

container.Configure(c =>
{
    foreach (var type in assembly.GetTypes()
        .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
    {
        c.Export(type).As<IPlugin>();
    }
});

var plugins = container.Locate<IEnumerable<IPlugin>>();
```

## Performance

Benchmarks comparing SSDI against popular DI containers (Windows 11):

### Singleton Resolution
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| Grace | 4.77 ns | 2.91 ns | - |
| **SSDI** | **6.14 ns** | **3.51 ns** | **-** |
| MS.DI | 6.10 ns | 4.33 ns | - |
| DryIoc | 5.56 ns | 4.78 ns | - |
| SimpleInj | 7.96 ns | 5.16 ns | - |
| Autofac | 100.16 ns | 87.64 ns | 808 B |

### Transient Resolution
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| Grace | 66.75 ns | 42.34 ns | 240 B |
| **SSDI** | **65.05 ns** | **59.30 ns** | **240 B** |
| DryIoc | 75.99 ns | 69.39 ns | 240 B |
| MS.DI | 81.43 ns | 70.71 ns | 240 B |
| SimpleInj | 106.58 ns | 85.19 ns | 240 B |
| Autofac | 1,445.70 ns | 1,346.16 ns | 8,320 B |

### Combined (Singleton + Transient)
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| Grace | 8.95 ns | 7.61 ns | 56 B |
| MS.DI | 11.19 ns | 9.21 ns | 56 B |
| DryIoc | 10.90 ns | 9.59 ns | 56 B |
| SimpleInj | 13.27 ns | 9.77 ns | 56 B |
| **SSDI** | **21.58 ns** | **17.46 ns** | **56 B** |
| Autofac | 354.18 ns | 293.20 ns | 1,720 B |

### Complex Graph Resolution
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| Grace | 17.81 ns | 15.96 ns | 136 B |
| MS.DI | 18.30 ns | 16.01 ns | 136 B |
| SimpleInj | 23.23 ns | 17.50 ns | 136 B |
| DryIoc | 18.55 ns | 17.63 ns | 136 B |
| **SSDI** | **70.57 ns** | **51.65 ns** | **136 B** |
| Autofac | 1,188.41 ns | 912.56 ns | 4,384 B |

### Container Setup (Registration)
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| DryIoc | 1.03 μs | 0.83 μs | 4.0 KB |
| MS.DI | 1.33 μs | 1.45 μs | 12.4 KB |
| Grace | 4.87 μs | 4.24 μs | 21.3 KB |
| SimpleInj | 14.96 μs | 14.35 μs | 55.6 KB |
| Autofac | 16.73 μs | 14.58 μs | 73.8 KB |
| **SSDI** | **18.28 μs** | **20.96 μs** | **23.3 KB** |


## License

MIT License - see [LICENSE](LICENSE) for details.
