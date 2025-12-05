# Super Simple Dependency Injection (SSDI)

[![NuGet](https://img.shields.io/nuget/v/SSDI.svg)](https://www.nuget.org/packages/SSDI/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, high-performance dependency injection framework for .NET designed for games and console applications. **No container build step required** — register types at any point during your application's lifecycle.

> **[Full Documentation](../../wiki)** — Comprehensive guides, API reference, and advanced features.

## Features

- **No build step** — Register types dynamically at runtime
- **Fast resolution** — Optimized for game loops and high-frequency calls
- **Thread-safe** — Lock-free resolution path, safe for multi-threaded environments.
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
| Grace | 4.77 ns | 2.89 ns | - |
| **SSDI** | **6.14 ns** | **3.45 ns** | **-** |
| SimpleInj | 7.96 ns | 4.79 ns | - |
| DryIoc | 5.56 ns | 4.99 ns | - |
| MS.DI | 6.10 ns | 5.82 ns | - |
| Autofac | 100.16 ns | 74.12 ns | 808 B |

### Transient Resolution
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| Grace | 66.75 ns | 48.81 ns | 240 B |
| **SSDI** | **65.05 ns** | **58.42 ns** | **240 B** |
| DryIoc | 75.99 ns | 67.45 ns | 240 B |
| MS.DI | 81.43 ns | 69.00 ns | 240 B |
| SimpleInj | 106.58 ns | 86.25 ns | 240 B |
| Autofac | 1,445.70 ns | 1,179.15 ns | 8,320 B |

### Combined (Singleton + Transient)
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| Grace | 8.95 ns | 7.49 ns | 56 B |
| MS.DI | 11.19 ns | 9.27 ns | 56 B |
| DryIoc | 10.90 ns | 9.71 ns | 56 B |
| SimpleInj | 13.27 ns | 9.85 ns | 56 B |
| **SSDI** | **21.58 ns** | **12.66 ns** | **56 B** |
| Autofac | 354.18 ns | 271.40 ns | 1,720 B |

### Complex Graph Resolution
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| MS.DI | 18.30 ns | 15.76 ns | 136 B |
| Grace | 17.81 ns | 16.09 ns | 136 B |
| DryIoc | 18.55 ns | 17.09 ns | 136 B |
| SimpleInj | 23.23 ns | 17.42 ns | 136 B |
| **SSDI (eager)** | **-** | **19.42 ns** | **136 B** |
| **SSDI (lazy)** | **70.57 ns** | **41.28 ns** | **136 B** |
| Autofac | 1,188.41 ns | 846.74 ns | 4,384 B |

### Container Setup (Registration)
| Container | .NET 8 | .NET 10 | Allocated |
|-----------|-------:|--------:|----------:|
| DryIoc | 1.03 μs | 0.82 μs | 4.0 KB |
| MS.DI | 1.33 μs | 1.42 μs | 12.4 KB |
| Grace | 4.87 μs | 4.23 μs | 21.3 KB |
| SimpleInj | 14.96 μs | 14.29 μs | 55.6 KB |
| Autofac | 16.73 μs | 14.48 μs | 73.8 KB |
| **SSDI (lazy)** | **18.28 μs** | **26.91 μs** | **30.7 KB** |
| **SSDI (eager)** | **-** | **10.40 ms** | **485 KB** |

> **Note:** SSDI supports two compilation modes:
> - **Lazy (default)** — Fast registration (~27μs), compiles factories on first `Locate<T>()` call. Best for hot-swapping and plugin systems.
> - **Eager** — Factories are pre-compiled during `Configure()` (~10ms), but resolution speed rivals top containers (~19ns). Set `EagerCompilation = true`.


## License

MIT License - see [LICENSE](LICENSE) for details.
