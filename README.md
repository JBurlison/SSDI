# Super Simple Dependency Injection (SSDI)

[![NuGet](https://img.shields.io/nuget/v/SSDI.svg)](https://www.nuget.org/packages/SSDI/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight dependency injection framework for .NET designed for console applications and games. Unlike traditional DI containers, **SSDI does not require a "build" step** — you can add registrations at any point during your application's lifecycle, making it perfect for plugin systems and dynamically loaded assemblies.

## Features

- ✅ **No container build step** — Register types at any time
- ✅ **Runtime extensibility** — Perfect for plugin architectures
- ✅ **Unregister support** — Remove registrations and hot-swap implementations
- ✅ **Lightweight** — Minimal overhead, ideal for games
- ✅ **Simple API** — Easy to learn and use
- ✅ **Multiple parameter binding options** — By type, name, or position

## Installation

```bash
dotnet add package SSDI
```

## Quick Start

```cs
var container = new DependencyInjectionContainer();

container.Configure(c =>
{
    c.Export<MyService>();
    c.Export<MyRepository>().Lifestyle.Singleton();
});

var service = container.Locate<MyService>();
```

## Lifecycle Management

SSDI supports two lifestyles:

| Lifestyle | Description |
|-----------|-------------|
| **Transient** (default) | Creates a new instance every time it's resolved |
| **Singleton** | Creates one instance for the application lifetime |

```cs
container.Configure(c =>
{
    c.Export<GameEngine>().Lifestyle.Singleton();  // One instance
    c.Export<Enemy>().Lifestyle.Transient();       // New instance each time (default)
    c.Export<Projectile>();                         // Transient is the default
});
```

## Registering Instances

You can register pre-built instances directly:

```cs
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

container.Configure(c =>
{
    // Register an existing instance (always treated as Singleton)
    c.ExportInstance(configuration).As<IConfiguration>();
});
```

## Interface Registration (Aliases)

**Important:** SSDI does not automatically discover interfaces. You must explicitly register aliases for any interfaces you want to resolve by:

```cs
container.Configure(c =>
{
    // Register implementations with their interfaces
    c.Export<SqlRepository>().As<IRepository>();
    c.Export<FileLogger>().As<ILogger>();
    
    // Multiple implementations of the same interface
    c.Export<AuthPacketHandler>().As<IPacketHandler>();
    c.Export<GamePacketHandler>().As<IPacketHandler>();
    c.Export<ChatPacketHandler>().As<IPacketHandler>();
});

// Resolve a single implementation
var logger = container.Locate<ILogger>();

// Resolve ALL implementations of an interface
var handlers = container.Locate<IEnumerable<IPacketHandler>>();
foreach (var handler in handlers)
{
    handler.Initialize();
}
```

## Constructor Parameter Injection

SSDI provides multiple ways to specify constructor parameters:

### By Type
```cs
container.Configure(c =>
{
    c.Export<GameServer>()
        .WithCtorParam<int>(8080)           // Matches any int parameter
        .WithCtorParam<string>("GameServer") // Matches any string parameter
        .Lifestyle.Singleton();
});
```

### By Parameter Name
```cs
container.Configure(c =>
{
    // Constructor: GameServer(string serverName, int port, bool enableLogging)
    c.Export<GameServer>()
        .WithCtorParam("serverName", "MyServer")
        .WithCtorParam("port", 8080)
        .WithCtorParam("enableLogging", true)
        .Lifestyle.Singleton();
});
```

### By Position (0-based)
```cs
container.Configure(c =>
{
    // Constructor: GameServer(string serverName, int port, bool enableLogging)
    c.Export<GameServer>()
        .WithCtorParam(0, "MyServer")  // Position 0: serverName
        .WithCtorParam(1, 8080)        // Position 1: port
        .WithCtorParam(2, true)        // Position 2: enableLogging
        .Lifestyle.Singleton();
});
```

### Multiple Positional Parameters at Once
```cs
container.Configure(c =>
{
    // Provide multiple params starting at position 0
    c.Export<GameServer>()
        .WithCtorPositionalParams("MyServer", 8080, true)
        .Lifestyle.Singleton();
});
```

## Locating Services with Parameters

You can also provide parameters when resolving:

```cs
// Simple locate
var server = container.Locate<TCPServer>();

// With a single positional parameter
var server = container.Locate<TCPServer>(0, "127.0.0.1");

// With positional parameters (starting at position 0)
var server = container.LocateWithPositionalParams<TCPServer>("127.0.0.1", 8080);

// With typed parameters (matched by type)
var server = container.LocateWithTypedParams<TCPServer>("127.0.0.1", 8080);

// With named parameters
var server = container.LocateWithNamedParameters<TCPServer>(
    ("address", "127.0.0.1"), 
    ("port", 8080)
);
```

## Unregistering Services

SSDI supports removing registrations at runtime, which is useful for plugin unloading, hot-swapping implementations, or cleaning up resources.

### Unregister a Specific Type
```cs
// Unregister a type and remove it from all aliases (interfaces)
container.Unregister<OldService>();

// Unregister a type but keep it in alias sets
container.Unregister<OldService>(removeFromAliases: false);
```

### Unregister All Implementations of an Interface
```cs
// Remove all implementations registered under IPacketHandler
int count = container.UnregisterAll<IPacketHandler>();
Console.WriteLine($"Removed {count} packet handlers");
```

### Check if a Type is Registered
```cs
if (container.IsRegistered<ILogger>())
{
    var logger = container.Locate<ILogger>();
}
```

### Example: Hot-Swapping a Service
```cs
// Initial registration
container.Configure(c =>
{
    c.Export<FileLogger>().As<ILogger>().Lifestyle.Singleton();
});

var logger = container.Locate<ILogger>();
logger.Log("Using file logger");

// Hot-swap to a different implementation
container.Unregister<FileLogger>(removeFromAliases: true);
container.Configure(c =>
{
    c.Export<CloudLogger>().As<ILogger>().Lifestyle.Singleton();
});

// New logger is now in use
logger = container.Locate<ILogger>();
logger.Log("Now using cloud logger");
```

### Example: Plugin Unloading
```cs
public class PluginManager
{
    private readonly DependencyInjectionContainer _container;
    private readonly Dictionary<string, List<Type>> _pluginTypes = new();

    public void LoadPlugin(string pluginId, Assembly assembly)
    {
        var types = new List<Type>();
        
        _container.Configure(c =>
        {
            foreach (var type in assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
            {
                c.Export(type).As<IPlugin>();
                types.Add(type);
            }
        });
        
        _pluginTypes[pluginId] = types;
    }

    public void UnloadPlugin(string pluginId)
    {
        if (_pluginTypes.TryGetValue(pluginId, out var types))
        {
            foreach (var type in types)
            {
                // Unregister each type (disposes singletons automatically)
                _container.Unregister(type);
            }
            _pluginTypes.Remove(pluginId);
        }
    }
}
```

### Example: Resource Cleanup on Scene Change
```cs
public class SceneManager
{
    private readonly DependencyInjectionContainer _container;

    public void UnloadCurrentScene()
    {
        // Unregister all scene-specific services
        // Singletons implementing IDisposable are automatically disposed
        _container.UnregisterAll<ISceneService>();
        _container.UnregisterAll<IEnemy>();
        _container.Unregister<CurrentSceneController>();
    }

    public void LoadBattleScene()
    {
        UnloadCurrentScene();
        
        _container.Configure(c =>
        {
            c.Export<BattleSceneController>().As<ISceneService>().Lifestyle.Singleton();
            c.Export<BattleUI>().As<ISceneService>().Lifestyle.Singleton();
            c.Export<Goblin>().As<IEnemy>();
            c.Export<Orc>().As<IEnemy>();
            c.Export<Dragon>().As<IEnemy>();
        });
    }

    public void LoadTownScene()
    {
        UnloadCurrentScene();
        
        _container.Configure(c =>
        {
            c.Export<TownSceneController>().As<ISceneService>().Lifestyle.Singleton();
            c.Export<TownUI>().As<ISceneService>().Lifestyle.Singleton();
            c.Export<ShopKeeper>().As<INPC>();
            c.Export<QuestGiver>().As<INPC>();
        });
    }
}
```

### Example: A/B Testing with Runtime Switching
```cs
public class ABTestManager
{
    private readonly DependencyInjectionContainer _container;

    public void SwitchToVariantA()
    {
        // Remove current payment processor
        _container.UnregisterAll<IPaymentProcessor>();
        
        _container.Configure(c =>
        {
            c.Export<StripeProcessor>().As<IPaymentProcessor>().Lifestyle.Singleton();
        });
    }

    public void SwitchToVariantB()
    {
        // Remove current payment processor
        _container.UnregisterAll<IPaymentProcessor>();
        
        _container.Configure(c =>
        {
            c.Export<PayPalProcessor>().As<IPaymentProcessor>().Lifestyle.Singleton();
        });
    }
}
```

### Example: Graceful Service Replacement
```cs
public class ServiceManager
{
    private readonly DependencyInjectionContainer _container;

    public async Task UpgradeDatabaseConnectionAsync(string newConnectionString)
    {
        // Check if we have an existing connection
        if (_container.IsRegistered<IDatabaseConnection>())
        {
            // Unregister will dispose the old connection gracefully
            _container.UnregisterAll<IDatabaseConnection>();
        }

        // Register the new connection
        _container.Configure(c =>
        {
            c.Export<DatabaseConnection>()
                .WithCtorParam("connectionString", newConnectionString)
                .As<IDatabaseConnection>()
                .Lifestyle.Singleton();
        });

        // Verify the new connection works
        var db = _container.Locate<IDatabaseConnection>();
        await db.TestConnectionAsync();
    }
}
```

### Example: Debug Mode Toggle
```cs
public class DebugManager
{
    private readonly DependencyInjectionContainer _container;
    private bool _debugMode = false;

    public void ToggleDebugMode()
    {
        _debugMode = !_debugMode;
        
        // Remove current logger
        _container.UnregisterAll<ILogger>();
        
        if (_debugMode)
        {
            _container.Configure(c =>
            {
                // Verbose console logger for debugging
                c.Export<ConsoleLogger>()
                    .WithCtorParam("minLevel", LogLevel.Trace)
                    .As<ILogger>()
                    .Lifestyle.Singleton();
                    
                // Add debug-only services
                c.Export<PerformanceProfiler>().Lifestyle.Singleton();
                c.Export<MemoryTracker>().Lifestyle.Singleton();
            });
        }
        else
        {
            _container.Configure(c =>
            {
                // Minimal file logger for production
                c.Export<FileLogger>()
                    .WithCtorParam("minLevel", LogLevel.Warning)
                    .As<ILogger>()
                    .Lifestyle.Singleton();
            });
            
            // Remove debug services
            _container.Unregister<PerformanceProfiler>();
            _container.Unregister<MemoryTracker>();
        }
    }
}
```

### Example: Connection Pool Management
```cs
public class ConnectionPoolManager
{
    private readonly DependencyInjectionContainer _container;
    private readonly List<Type> _connectionTypes = new();

    public void AddConnection<TConnection>() where TConnection : IConnection
    {
        _container.Configure(c =>
        {
            c.Export<TConnection>().As<IConnection>();
        });
        _connectionTypes.Add(typeof(TConnection));
    }

    public void RemoveConnection<TConnection>() where TConnection : IConnection
    {
        if (_container.Unregister<TConnection>())
        {
            _connectionTypes.Remove(typeof(TConnection));
            Console.WriteLine($"Removed connection: {typeof(TConnection).Name}");
        }
    }

    public void ClearAllConnections()
    {
        int removed = _container.UnregisterAll<IConnection>();
        _connectionTypes.Clear();
        Console.WriteLine($"Cleared {removed} connections");
    }

    public IEnumerable<IConnection> GetActiveConnections()
    {
        return _container.Locate<IEnumerable<IConnection>>();
    }
}
```

> **Note:** When unregistering a singleton that has already been instantiated, SSDI will automatically call `Dispose()` if the instance implements `IDisposable` (or `IAsyncDisposable` on .NET 8+).

---

## Dynamic Registration Throughout Application Lifecycle

One of SSDI's key features is the ability to add registrations at any time. This is essential for:

- **Plugin systems** — Load plugins and register their services dynamically
- **Game mods** — Allow mods to extend the DI container
- **Feature toggles** — Conditionally register services based on configuration
- **Hot reloading** — Update services without restarting the application

### Example: Plugin System

```cs
public class Game
{
    private readonly DependencyInjectionContainer _container;

    public Game()
    {
        _container = new DependencyInjectionContainer();
        
        // Initial registration - core services
        _container.Configure(c =>
        {
            c.Export<GameEngine>().Lifestyle.Singleton();
            c.Export<InputManager>().Lifestyle.Singleton();
            c.Export<AudioManager>().Lifestyle.Singleton();
            c.ExportInstance(LoadConfiguration()).As<IGameConfig>();
        });
    }

    public void LoadPlugin(string pluginPath)
    {
        // Load plugin assembly at runtime
        var assembly = Assembly.LoadFrom(pluginPath);
        
        // Plugin can register its own services
        _container.Configure(c =>
        {
            // Discover and register all IGamePlugin implementations from the plugin
            foreach (var type in assembly.GetTypes()
                .Where(t => typeof(IGamePlugin).IsAssignableFrom(t) && !t.IsAbstract))
            {
                c.Export(type).As<IGamePlugin>();
            }
        });
        
        // Resolve all plugins (including newly loaded ones)
        var plugins = _container.Locate<IEnumerable<IGamePlugin>>();
        foreach (var plugin in plugins)
        {
            plugin.Initialize();
        }
    }
}
```

### Example: Multi-Stage Server Setup

```cs
public class ServerApplication
{
    private readonly DependencyInjectionContainer _container = new();

    public async Task StartAsync()
    {
        // Stage 1: Core infrastructure
        _container.Configure(c =>
        {
            c.Export<Logger>().As<ILogger>().Lifestyle.Singleton();
            c.Export<ConfigurationService>().Lifestyle.Singleton();
        });

        var config = _container.Locate<ConfigurationService>();
        await config.LoadAsync();

        // Stage 2: Database (depends on configuration)
        _container.Configure(c =>
        {
            c.Export<DatabaseConnection>()
                .WithCtorParam("connectionString", config.ConnectionString)
                .Lifestyle.Singleton();
            c.Export<UserRepository>().As<IUserRepository>();
            c.Export<OrderRepository>().As<IOrderRepository>();
        });

        // Stage 3: Network services
        _container.Configure(c =>
        {
            c.Export<TCPServer>()
                .WithCtorParam("port", config.Port)
                .Lifestyle.Singleton();
            c.Export<AuthHandler>().As<IPacketHandler>();
            c.Export<GameHandler>().As<IPacketHandler>();
        });

        // Stage 4: Optional features based on config
        if (config.EnableMetrics)
        {
            _container.Configure(c =>
            {
                c.Export<MetricsCollector>().As<IMetricsCollector>().Lifestyle.Singleton();
                c.Export<PrometheusExporter>().Lifestyle.Singleton();
            });
        }

        // Start the server
        var server = _container.Locate<TCPServer>();
        await server.StartAsync();
    }
}
```

### Example: Game Scene Management

```cs
public class SceneManager
{
    private readonly DependencyInjectionContainer _container;

    public SceneManager(DependencyInjectionContainer container)
    {
        _container = container;
    }

    public void LoadMainMenu()
    {
        _container.Configure(c =>
        {
            c.Export<MainMenuUI>().Lifestyle.Singleton();
            c.Export<MenuInputHandler>().As<IInputHandler>();
            c.Export<MenuAudioController>().As<IAudioController>();
        });
    }

    public void LoadGameLevel(int levelNumber)
    {
        _container.Configure(c =>
        {
            c.Export<GameLevelUI>().Lifestyle.Singleton();
            c.Export<PlayerController>().Lifestyle.Singleton();
            c.Export<EnemySpawner>()
                .WithCtorParam("levelNumber", levelNumber)
                .Lifestyle.Singleton();
            c.Export<GameInputHandler>().As<IInputHandler>();
            c.Export<GameAudioController>().As<IAudioController>();
            
            // Level-specific enemies
            c.Export<Zombie>().As<IEnemy>();
            c.Export<Skeleton>().As<IEnemy>();
            if (levelNumber >= 5)
            {
                c.Export<Boss>().As<IEnemy>();
            }
        });

        var spawner = _container.Locate<EnemySpawner>();
        var enemies = _container.Locate<IEnumerable<IEnemy>>();
        spawner.RegisterEnemyTypes(enemies);
    }
}
```

### Example: Conditional Registration with Feature Flags

```cs
public void ConfigureServices(FeatureFlags features)
{
    // Always register core services
    _container.Configure(c =>
    {
        c.Export<CoreService>().Lifestyle.Singleton();
    });

    // Conditionally register based on features
    if (features.UseNewRenderer)
    {
        _container.Configure(c =>
        {
            c.Export<VulkanRenderer>().As<IRenderer>().Lifestyle.Singleton();
        });
    }
    else
    {
        _container.Configure(c =>
        {
            c.Export<OpenGLRenderer>().As<IRenderer>().Lifestyle.Singleton();
        });
    }

    if (features.EnableMultiplayer)
    {
        _container.Configure(c =>
        {
            c.Export<NetworkManager>().Lifestyle.Singleton();
            c.Export<LobbyService>().Lifestyle.Singleton();
            c.Export<MatchmakingService>().Lifestyle.Singleton();
        });
    }

    if (features.EnableModding)
    {
        _container.Configure(c =>
        {
            c.Export<ModLoader>().Lifestyle.Singleton();
            c.Export<ModRegistry>().Lifestyle.Singleton();
        });
        
        // Let mods register their services
        var modLoader = _container.Locate<ModLoader>();
        foreach (var mod in modLoader.LoadMods())
        {
            mod.RegisterServices(_container);
        }
    }
}
```

### Example: Testing with Mock Registrations

```cs
[TestClass]
public class GameEngineTests
{
    private DependencyInjectionContainer _container;

    [TestInitialize]
    public void Setup()
    {
        _container = new DependencyInjectionContainer();
        
        // Register real services
        _container.Configure(c =>
        {
            c.Export<GameEngine>().Lifestyle.Singleton();
            c.Export<PhysicsEngine>().Lifestyle.Singleton();
        });
    }

    [TestMethod]
    public void TestWithMockInput()
    {
        // Override with mock for this test
        _container.Configure(c =>
        {
            c.ExportInstance(new MockInputManager()).As<IInputManager>();
        });

        var engine = _container.Locate<GameEngine>();
        // Test with mock input...
    }

    [TestMethod]
    public void TestWithMockAudio()
    {
        // Different mock for this test
        _container.Configure(c =>
        {
            c.ExportInstance(new SilentAudioManager()).As<IAudioManager>();
        });

        var engine = _container.Locate<GameEngine>();
        // Test without audio...
    }
}
```

---

## Full Example: Game Server

```cs
public class Program
{
    public static async Task Main(string[] args)
    {
        var container = new DependencyInjectionContainer();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Core registrations
        container.Configure(c =>
        {
            // Configuration instance
            c.ExportInstance(configuration).As<IConfiguration>();
            
            // Core services
            c.Export<TCPServer>().Lifestyle.Singleton();
            c.Export<PacketRouter>().Lifestyle.Singleton();
            
            // Packet handlers (multiple implementations)
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
            c.Export<ChatPacketHandler>().As<IPacketHandler>();
            
            // Repositories
            c.Export<PlayerRepository>().As<IPlayerRepository>();
            c.Export<InventoryRepository>().As<IInventoryRepository>();
        });

        // Load plugins
        var pluginsPath = Path.Combine(Directory.GetCurrentDirectory(), "plugins");
        if (Directory.Exists(pluginsPath))
        {
            foreach (var pluginDll in Directory.GetFiles(pluginsPath, "*.dll"))
            {
                var assembly = Assembly.LoadFrom(pluginDll);
                
                container.Configure(c =>
                {
                    foreach (var handlerType in assembly.GetTypes()
                        .Where(t => typeof(IPacketHandler).IsAssignableFrom(t) && !t.IsAbstract))
                    {
                        c.Export(handlerType).As<IPacketHandler>();
                    }
                });
                
                Console.WriteLine($"Loaded plugin: {Path.GetFileName(pluginDll)}");
            }
        }

        // Initialize all packet handlers
        var router = container.Locate<PacketRouter>();
        var handlers = container.Locate<IEnumerable<IPacketHandler>>();
        foreach (var handler in handlers)
        {
            router.RegisterHandler(handler);
        }

        // Start server
        var server = container.Locate<TCPServer>();
        await server.StartAsync();
    }
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

