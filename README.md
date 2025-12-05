# Super Simple Dependency Injection (SSDI)

[![NuGet](https://img.shields.io/nuget/v/SSDI.svg)](https://www.nuget.org/packages/SSDI/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, high-performance dependency injection framework for .NET designed for games and console applications. **No container build step required** — register types at any point during your application's lifecycle.

> **[Full Documentation](../../wiki)** — Comprehensive guides, API reference, and advanced features.

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
<table>
<tr><th>Container</th><th align="right">.NET 8</th><th align="right">.NET 10</th><th align="right">Allocated</th></tr>
<tr><td>Grace</td><td align="right">4.77 ns</td><td align="right">2.89 ns</td><td align="right">-</td></tr>
<tr style="background-color:#c9c9c9; color:black"><td><b>SSDI</b></td><td align="right"><b>6.14 ns</b></td><td align="right"><b>3.45 ns</b></td><td align="right"><b>-</b></td></tr>
<tr><td>SimpleInj</td><td align="right">7.96 ns</td><td align="right">4.79 ns</td><td align="right">-</td></tr>
<tr><td>DryIoc</td><td align="right">5.56 ns</td><td align="right">4.99 ns</td><td align="right">-</td></tr>
<tr><td>MS.DI</td><td align="right">6.10 ns</td><td align="right">5.82 ns</td><td align="right">-</td></tr>
<tr><td>Autofac</td><td align="right">100.16 ns</td><td align="right">74.12 ns</td><td align="right">808 B</td></tr>
</table>

### Transient Resolution
<table>
<tr><th>Container</th><th align="right">.NET 8</th><th align="right">.NET 10</th><th align="right">Allocated</th></tr>
<tr><td>Grace</td><td align="right">66.75 ns</td><td align="right">48.81 ns</td><td align="right">240 B</td></tr>
<tr style="background-color:#c9c9c9; color:black"><td><b>SSDI</b></td><td align="right"><b>65.05 ns</b></td><td align="right"><b>58.42 ns</b></td><td align="right"><b>240 B</b></td></tr>
<tr><td>DryIoc</td><td align="right">75.99 ns</td><td align="right">67.45 ns</td><td align="right">240 B</td></tr>
<tr><td>MS.DI</td><td align="right">81.43 ns</td><td align="right">69.00 ns</td><td align="right">240 B</td></tr>
<tr><td>SimpleInj</td><td align="right">106.58 ns</td><td align="right">86.25 ns</td><td align="right">240 B</td></tr>
<tr><td>Autofac</td><td align="right">1,445.70 ns</td><td align="right">1,179.15 ns</td><td align="right">8,320 B</td></tr>
</table>

### Combined (Singleton + Transient)
<table>
<tr><th>Container</th><th align="right">.NET 8</th><th align="right">.NET 10</th><th align="right">Allocated</th></tr>
<tr><td>Grace</td><td align="right">8.95 ns</td><td align="right">7.49 ns</td><td align="right">56 B</td></tr>
<tr><td>MS.DI</td><td align="right">11.19 ns</td><td align="right">9.27 ns</td><td align="right">56 B</td></tr>
<tr><td>DryIoc</td><td align="right">10.90 ns</td><td align="right">9.71 ns</td><td align="right">56 B</td></tr>
<tr><td>SimpleInj</td><td align="right">13.27 ns</td><td align="right">9.85 ns</td><td align="right">56 B</td></tr>
<tr style="background-color:#c9c9c9; color:black"><td><b>SSDI</b></td><td align="right"><b>21.58 ns</b></td><td align="right"><b>12.66 ns</b></td><td align="right"><b>56 B</b></td></tr>
<tr><td>Autofac</td><td align="right">354.18 ns</td><td align="right">271.40 ns</td><td align="right">1,720 B</td></tr>
</table>

### Complex Graph Resolution
<table>
<tr><th>Container</th><th align="right">.NET 8</th><th align="right">.NET 10</th><th align="right">Allocated</th></tr>
<tr><td>MS.DI</td><td align="right">18.30 ns</td><td align="right">15.76 ns</td><td align="right">136 B</td></tr>
<tr><td>Grace</td><td align="right">17.81 ns</td><td align="right">16.09 ns</td><td align="right">136 B</td></tr>
<tr><td>DryIoc</td><td align="right">18.55 ns</td><td align="right">17.09 ns</td><td align="right">136 B</td></tr>
<tr><td>SimpleInj</td><td align="right">23.23 ns</td><td align="right">17.42 ns</td><td align="right">136 B</td></tr>
<tr style="background-color:#c9c9c9; color:black"><td><b>SSDI (eager)</b></td><td align="right"><b>-</b></td><td align="right"><b>19.42 ns</b></td><td align="right"><b>136 B</b></td></tr>
<tr style="background-color:#c9c9c9; color:black"><td><b>SSDI (lazy)</b></td><td align="right"><b>70.57 ns</b></td><td align="right"><b>41.28 ns</b></td><td align="right"><b>136 B</b></td></tr>
<tr><td>Autofac</td><td align="right">1,188.41 ns</td><td align="right">846.74 ns</td><td align="right">4,384 B</td></tr>
</table>

### Container Setup (Registration)
<table>
<tr><th>Container</th><th align="right">.NET 8</th><th align="right">.NET 10</th><th align="right">Allocated</th></tr>
<tr><td>DryIoc</td><td align="right">1.03 μs</td><td align="right">0.82 μs</td><td align="right">4.0 KB</td></tr>
<tr><td>MS.DI</td><td align="right">1.33 μs</td><td align="right">1.42 μs</td><td align="right">12.4 KB</td></tr>
<tr><td>Grace</td><td align="right">4.87 μs</td><td align="right">4.23 μs</td><td align="right">21.3 KB</td></tr>
<tr><td>SimpleInj</td><td align="right">14.96 μs</td><td align="right">14.29 μs</td><td align="right">55.6 KB</td></tr>
<tr><td>Autofac</td><td align="right">16.73 μs</td><td align="right">14.48 μs</td><td align="right">73.8 KB</td></tr>
<tr style="background-color:#c9c9c9; color:black"><td><b>SSDI (lazy)</b></td><td align="right"><b>18.28 μs</b></td><td align="right"><b>26.91 μs</b></td><td align="right"><b>30.7 KB</b></td></tr>
<tr style="background-color:#c9c9c9; color:black"><td><b>SSDI (eager)</b></td><td align="right"><b>-</b></td><td align="right"><b>10.40 ms</b></td><td align="right"><b>485 KB</b></td></tr>
</table>

> **Note:** SSDI supports two compilation modes:
> - **Lazy (default)** — Fast registration, compiles factories on first resolution. Best for hot-swapping.
> - **Eager** — Slow registration, but resolution speed rivals top containers. Set `EagerCompilation = true`.


## License

MIT License - see [LICENSE](LICENSE) for details.
