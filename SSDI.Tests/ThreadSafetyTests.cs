using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;

namespace SSDI.Tests;

[TestClass]
public class ThreadSafetyTests
{
    private const int ThreadCount = 10;
    private const int IterationsPerThread = 1000;

    #region Concurrent Resolution Tests

    [TestMethod]
    public void ConcurrentLocate_Transient_AllThreadsGetInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        var results = new ConcurrentBag<SimpleService>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < IterationsPerThread; i++)
            {
                try
                {
                    var instance = container.Locate<SimpleService>();
                    results.Add(instance);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsTrue(exceptions.IsEmpty, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount * IterationsPerThread, results);

        // Transient should create unique instances
        var uniqueInstances = results.Distinct().Count();
        Assert.AreEqual(ThreadCount * IterationsPerThread, uniqueInstances);
    }

    [TestMethod]
    public void ConcurrentLocate_Singleton_AllThreadsGetSameInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());

        var results = new ConcurrentBag<SimpleService>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < IterationsPerThread; i++)
            {
                try
                {
                    var instance = container.Locate<SimpleService>();
                    results.Add(instance);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsTrue(exceptions.IsEmpty, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount * IterationsPerThread, results);

        // Singleton should return the same instance
        var uniqueInstances = results.Distinct().Count();
        Assert.AreEqual(1, uniqueInstances);
    }

    [TestMethod]
    public void ConcurrentLocate_WithDependencies_ResolvesCorrectly()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<ServiceWithDependency>();
        });

        var results = new ConcurrentBag<ServiceWithDependency>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < IterationsPerThread; i++)
            {
                try
                {
                    var instance = container.Locate<ServiceWithDependency>();
                    results.Add(instance);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsTrue(exceptions.IsEmpty, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount * IterationsPerThread, results);

        // All should have the same singleton dependency
        var singletonDependency = results.First().Dependency;
        Assert.IsTrue(results.All(r => ReferenceEquals(r.Dependency, singletonDependency)));
    }

    [TestMethod]
    public void ConcurrentLocate_ViaInterface_ResolvesCorrectly()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As<IService>().Lifestyle.Singleton());

        var results = new ConcurrentBag<IService>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < IterationsPerThread; i++)
            {
                try
                {
                    var instance = container.Locate<IService>();
                    results.Add(instance);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsTrue(exceptions.IsEmpty, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount * IterationsPerThread, results);

        // Singleton via interface
        var uniqueInstances = results.Distinct().Count();
        Assert.AreEqual(1, uniqueInstances);
    }

    [TestMethod]
    public void ConcurrentLocate_IEnumerable_ResolvesAllImplementations()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
            c.Export<ChatPacketHandler>().As<IPacketHandler>();
        });

        var results = new ConcurrentBag<IEnumerable<IPacketHandler>>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < IterationsPerThread; i++)
            {
                try
                {
                    var handlers = container.Locate<IEnumerable<IPacketHandler>>();
                    results.Add(handlers);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsTrue(exceptions.IsEmpty, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount * IterationsPerThread, results);

        // Each resolution should have 3 handlers
        Assert.IsTrue(results.All(r => r.Count() == 3));
    }

    #endregion

    #region Concurrent Registration Tests

    [TestMethod]
    public void ConcurrentRegistration_MultipleTypes_AllRegistered()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var exceptions = new ConcurrentBag<Exception>();
        var registrationCount = 100;

        // Act - Register different types concurrently
        Parallel.For(0, registrationCount, i =>
        {
            try
            {
                container.Configure(c =>
                {
                    // Each thread registers a different combination
                    if (i % 3 == 0)
                        c.Export<SimpleService>();
                    else if (i % 3 == 1)
                        c.Export<AnotherService>();
                    else
                        c.Export<ServiceImplementation>().As<IService>();
                });
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");

        // All types should be registered
        Assert.IsTrue(container.IsRegistered<SimpleService>());
        Assert.IsTrue(container.IsRegistered<AnotherService>());
        Assert.IsTrue(container.IsRegistered<IService>());
    }

    [TestMethod]
    public void ConcurrentRegistrationAndResolution_NoDeadlock()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act - Mix registration and resolution concurrently
        var tasks = new List<Task>();

        // Resolution threads
        for (var i = 0; i < ThreadCount / 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Locate<SimpleService>();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // Registration threads
        for (var i = 0; i < ThreadCount / 2; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Configure(c => c.Export<AnotherService>());
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // Let it run for a bit
        Thread.Sleep(1000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    #endregion

    #region Concurrent Unregistration Tests

    [TestMethod]
    public void ConcurrentUnregistration_NoExceptions()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var exceptions = new ConcurrentBag<Exception>();
        var unregisterResults = new ConcurrentBag<bool>();

        // Register many types
        for (var i = 0; i < 100; i++)
        {
            container.Configure(c =>
            {
                c.Export<SimpleService>();
                c.Export<AnotherService>();
            });
        }

        // Act - Unregister concurrently
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var result = container.Unregister<SimpleService>();
                    unregisterResults.Add(result);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void ConcurrentResolutionAndUnregistration_NoDeadlock()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Initial registration
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<AnotherService>().Lifestyle.Singleton();
        });

        // Act - Mix resolution and unregistration concurrently
        var tasks = new List<Task>();

        // Resolution threads
        for (var i = 0; i < ThreadCount / 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // May succeed or fail depending on timing
                        container.Locate<SimpleService>();
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected if type was unregistered
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // Unregistration and re-registration threads
        for (var i = 0; i < ThreadCount / 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Unregister<SimpleService>();
                        container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // Let it run for a bit
        Thread.Sleep(1000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert - no unexpected exceptions
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public async Task ConcurrentUnregisterAllAsync_NoExceptions()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var exceptions = new ConcurrentBag<Exception>();

        // Register multiple implementations
        container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
            c.Export<ChatPacketHandler>().As<IPacketHandler>();
        });

        // Act - Unregister all concurrently
        var tasks = Enumerable.Range(0, ThreadCount).Select(_ => Task.Run(async () =>
        {
            try
            {
                await container.UnregisterAllAsync<IPacketHandler>();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));

        await Task.WhenAll(tasks);

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void UnregisterWhileLocating_TransientService_HandlesGracefully()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var successfulResolutions = 0;
        var failedResolutions = 0;
        var unexpectedExceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        container.Configure(c => c.Export<SimpleService>());

        var tasks = new List<Task>();

        // High-frequency resolution threads
        for (var i = 0; i < 8; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var instance = container.Locate<SimpleService>();
                        if (instance != null)
                            Interlocked.Increment(ref successfulResolutions);
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected when type is unregistered
                        Interlocked.Increment(ref failedResolutions);
                    }
                    catch (Exception ex)
                    {
                        unexpectedExceptions.Add(ex);
                    }
                }
            }));
        }

        // Unregister/re-register thread
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    container.Unregister<SimpleService>();
                    Thread.Sleep(1); // Small delay to allow some failed resolutions
                    container.Configure(c => c.Export<SimpleService>());
                }
                catch (Exception ex)
                {
                    unexpectedExceptions.Add(ex);
                }
            }
        }));

        Thread.Sleep(2000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsTrue(unexpectedExceptions.IsEmpty,
            $"Unexpected exceptions: {string.Join(", ", unexpectedExceptions.Select(e => e.Message))}");
        Assert.AreNotEqual(0, successfulResolutions, "Should have some successful resolutions");
        // Failed resolutions are expected and acceptable
    }

    [TestMethod]
    public void UnregisterWhileLocating_SingletonService_HandlesGracefully()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var resolvedInstances = new ConcurrentBag<SimpleService>();
        var unexpectedExceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());

        var tasks = new List<Task>();

        // High-frequency resolution threads
        for (var i = 0; i < 8; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var instance = container.Locate<SimpleService>();
                        if (instance != null)
                            resolvedInstances.Add(instance);
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected when singleton is unregistered
                    }
                    catch (Exception ex)
                    {
                        unexpectedExceptions.Add(ex);
                    }
                }
            }));
        }

        // Unregister/re-register thread - creates new singleton each time
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    container.Unregister<SimpleService>();
                    container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());
                }
                catch (Exception ex)
                {
                    unexpectedExceptions.Add(ex);
                }
            }
        }));

        Thread.Sleep(2000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsTrue(unexpectedExceptions.IsEmpty,
            $"Unexpected exceptions: {string.Join(", ", unexpectedExceptions.Select(e => e.Message))}");
        Assert.IsFalse(resolvedInstances.IsEmpty, "Should have resolved some instances");
        // Multiple unique instances are expected since singleton gets recreated after unregister
    }

    [TestMethod]
    public void UnregisterWhileLocating_InterfaceAlias_HandlesGracefully()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var successfulResolutions = 0;
        var unexpectedExceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        container.Configure(c => c.Export<ServiceImplementation>().As<IService>());

        var tasks = new List<Task>();

        // Resolution via interface
        for (var i = 0; i < 8; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var instance = container.Locate<IService>();
                        if (instance != null)
                            Interlocked.Increment(ref successfulResolutions);
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected when unregistered
                    }
                    catch (Exception ex)
                    {
                        unexpectedExceptions.Add(ex);
                    }
                }
            }));
        }

        // Unregister/re-register thread
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    container.Unregister<ServiceImplementation>();
                    container.Configure(c => c.Export<ServiceImplementation>().As<IService>());
                }
                catch (Exception ex)
                {
                    unexpectedExceptions.Add(ex);
                }
            }
        }));

        Thread.Sleep(2000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsTrue(unexpectedExceptions.IsEmpty,
            $"Unexpected exceptions: {string.Join(", ", unexpectedExceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void UnregisterWhileLocating_IEnumerable_HandlesGracefully()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var resolvedCounts = new ConcurrentBag<int>();
        var unexpectedExceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
            c.Export<ChatPacketHandler>().As<IPacketHandler>();
        });

        var tasks = new List<Task>();

        // Resolution of IEnumerable
        for (var i = 0; i < 6; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var handlers = container.Locate<IEnumerable<IPacketHandler>>();
                        resolvedCounts.Add(handlers.Count());
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected during unregistration
                    }
                    catch (Exception ex)
                    {
                        unexpectedExceptions.Add(ex);
                    }
                }
            }));
        }

        // Unregister one handler at a time and re-register
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    container.Unregister<AuthPacketHandler>();
                    container.Configure(c => c.Export<AuthPacketHandler>().As<IPacketHandler>());
                }
                catch (Exception ex)
                {
                    unexpectedExceptions.Add(ex);
                }
            }
        }));

        // UnregisterAll and re-register
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    container.UnregisterAll<IPacketHandler>();
                    container.Configure(c =>
                    {
                        c.Export<AuthPacketHandler>().As<IPacketHandler>();
                        c.Export<GamePacketHandler>().As<IPacketHandler>();
                        c.Export<ChatPacketHandler>().As<IPacketHandler>();
                    });
                }
                catch (Exception ex)
                {
                    unexpectedExceptions.Add(ex);
                }
            }
        }));

        Thread.Sleep(2000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsTrue(unexpectedExceptions.IsEmpty,
            $"Unexpected exceptions: {string.Join(", ", unexpectedExceptions.Select(e => e.Message))}");
        Assert.IsFalse(resolvedCounts.IsEmpty, "Should have resolved some enumerables");
        // Count can vary (0-3) depending on timing, all are valid
    }

    [TestMethod]
    public void UnregisterWhileLocating_WithDependencies_HandlesGracefully()
    {
        // Arrange - Service with dependency where dependency gets unregistered
        var container = new DependencyInjectionContainer();
        var successfulResolutions = 0;
        var unexpectedExceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<ServiceWithDependency>();
        });

        var tasks = new List<Task>();

        // Resolve the service with dependency
        for (var i = 0; i < 6; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var instance = container.Locate<ServiceWithDependency>();
                        if (instance?.Dependency != null)
                            Interlocked.Increment(ref successfulResolutions);
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected when dependency is missing
                    }
                    catch (Exception ex)
                    {
                        unexpectedExceptions.Add(ex);
                    }
                }
            }));
        }

        // Unregister the dependency
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    container.Unregister<SimpleService>();
                    Thread.Sleep(1);
                    container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());
                }
                catch (Exception ex)
                {
                    unexpectedExceptions.Add(ex);
                }
            }
        }));

        Thread.Sleep(2000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsEmpty(unexpectedExceptions,
            $"Unexpected exceptions: {string.Join(", ", unexpectedExceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void UnregisterWhileLocating_DisposableSingleton_DisposesCorrectly()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var disposeCount = 0;
        var unexpectedExceptions = new ConcurrentBag<Exception>();

        container.Configure(c => c.Export<TrackingDisposableService>().Lifestyle.Singleton());
        TrackingDisposableService.OnDispose = () => Interlocked.Increment(ref disposeCount);

        // First, create the singleton
        var instance = container.Locate<TrackingDisposableService>();
        Assert.IsNotNull(instance);

        // Unregister should dispose it
        container.Unregister<TrackingDisposableService>();

        // Assert disposal happened
        Assert.AreEqual(1, disposeCount, "Singleton should have been disposed exactly once");

        // Now test concurrent scenario
        disposeCount = 0;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        container.Configure(c => c.Export<TrackingDisposableService>().Lifestyle.Singleton());

        var tasks = new List<Task>();

        // Resolution threads
        for (var i = 0; i < 4; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Locate<TrackingDisposableService>();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Could happen if we get instance just as it's being disposed
                    }
                    catch (InvalidOperationException)
                    {
                        // Expected when type is unregistered
                    }
                    catch (Exception ex)
                    {
                        unexpectedExceptions.Add(ex);
                    }
                }
            }));
        }

        // Unregister thread - should dispose singleton each time
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    // First, ensure there's a singleton to dispose
                    try { container.Locate<TrackingDisposableService>(); } catch { }

                    container.Unregister<TrackingDisposableService>();
                    container.Configure(c => c.Export<TrackingDisposableService>().Lifestyle.Singleton());
                }
                catch (Exception ex)
                {
                    unexpectedExceptions.Add(ex);
                }
            }
        }));

        Thread.Sleep(1500);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert - no unexpected exceptions during concurrent operations
        Assert.IsEmpty(unexpectedExceptions,
            $"Unexpected exceptions: {string.Join(", ", unexpectedExceptions.Select(e => e.Message))}");
        // Dispose count can vary in concurrent scenario, just verify no crashes
    }

    #endregion

    #region Scope Thread Safety Tests

    [TestMethod]
    public void ConcurrentScopeCreation_NoExceptions()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<ThreadSafeScopedService>().Lifestyle.Scoped();
            c.Export<SimpleService>().Lifestyle.Singleton();
        });

        var scopes = new ConcurrentBag<IScope>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, ThreadCount * 10, _ =>
        {
            try
            {
                var scope = container.CreateScope();
                scopes.Add(scope);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount * 10, scopes);

        // Cleanup
        foreach (var scope in scopes)
        {
            scope.Dispose();
        }
    }

    [TestMethod]
    public void ConcurrentResolutionWithinScope_SameScopedInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ThreadSafeScopedService>().Lifestyle.Scoped());

        var exceptions = new ConcurrentBag<Exception>();

        using var scope = container.CreateScope();
        var results = new ConcurrentBag<ThreadSafeScopedService>();

        // Act
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < IterationsPerThread; i++)
            {
                try
                {
                    var instance = scope.Locate<ThreadSafeScopedService>();
                    results.Add(instance);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount * IterationsPerThread, results);

        // All should be the same scoped instance
        var uniqueInstances = results.Distinct().Count();
        Assert.AreEqual(1, uniqueInstances);
    }

    [TestMethod]
    public void ConcurrentScopes_DifferentScopedInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ThreadSafeScopedService>().Lifestyle.Scoped());

        var instances = new ConcurrentBag<ThreadSafeScopedService>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act - Each thread creates its own scope
        Parallel.For(0, ThreadCount, _ =>
        {
            try
            {
                using var scope = container.CreateScope();
                var instance = scope.Locate<ThreadSafeScopedService>();
                instances.Add(instance);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(ThreadCount, instances);

        // Each scope should have a different instance
        var uniqueInstances = instances.Distinct().Count();
        Assert.AreEqual(ThreadCount, uniqueInstances);
    }

    #endregion

    #region IsRegistered Thread Safety Tests

    [TestMethod]
    public void ConcurrentIsRegistered_NoExceptions()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<ServiceImplementation>().As<IService>();
        });

        var results = new ConcurrentBag<bool>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        Parallel.For(0, ThreadCount, _ =>
        {
            for (var i = 0; i < IterationsPerThread; i++)
            {
                try
                {
                    var registered = container.IsRegistered<SimpleService>();
                    results.Add(registered);
                    registered = container.IsRegistered<IService>();
                    results.Add(registered);
                    registered = container.IsRegistered<AnotherService>();
                    results.Add(registered);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void ConcurrentIsRegisteredDuringRegistration_NoExceptions()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var tasks = new List<Task>();

        // IsRegistered threads
        for (var i = 0; i < ThreadCount / 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.IsRegistered<SimpleService>();
                        container.IsRegistered<IService>();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // Registration threads
        for (var i = 0; i < ThreadCount / 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Configure(c => c.Export<SimpleService>());
                        container.Configure(c => c.Export<ServiceImplementation>().As<IService>());
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        Thread.Sleep(1000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    #endregion

    #region Stress Tests

    [TestMethod]
    public void StressTest_AllOperationsConcurrently()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<AnotherService>();
        });

        var exceptions = new ConcurrentBag<Exception>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var tasks = new List<Task>();

        // Act - All operations concurrently
        // Resolution threads
        for (var i = 0; i < 3; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Locate<SimpleService>();
                        container.Locate<AnotherService>();
                    }
                    catch (InvalidOperationException) { }
                    catch (Exception ex) { exceptions.Add(ex); }
                }
            }));
        }

        // Registration threads
        for (var i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Configure(c => c.Export<ServiceImplementation>().As<IService>());
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                }
            }));
        }

        // Unregistration threads
        for (var i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.Unregister<ServiceImplementation>();
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                }
            }));
        }

        // IsRegistered threads
        for (var i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        container.IsRegistered<SimpleService>();
                        container.IsRegistered<IService>();
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                }
            }));
        }

        // Scope threads
        for (var i = 0; i < 2; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = container.CreateScope();
                        // Scopes should work even during container modifications
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                }
            }));
        }

        Thread.Sleep(2000);
        cts.Cancel();
        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
    }

    [TestMethod]
    public void StressTest_RapidSingletonCreation()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<CountingService>().Lifestyle.Singleton());

        CountingService.ResetCount();
        var results = new ConcurrentBag<CountingService>();
        var exceptions = new ConcurrentBag<Exception>();

        // Act - Many threads trying to get the singleton at once
        Parallel.For(0, 100, _ =>
        {
            try
            {
                var instance = container.Locate<CountingService>();
                results.Add(instance);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.HasCount(100, results);

        // Only one instance should have been created
        Assert.AreEqual(1, CountingService.InstanceCount);

        // All results should be the same instance
        var uniqueInstances = results.Distinct().Count();
        Assert.AreEqual(1, uniqueInstances);
    }

    #endregion

    #region Event Thread Safety Tests

    [TestMethod]
    public void ConcurrentRegistration_EventsFire_NoExceptions()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var eventCount = 0;
        var exceptions = new ConcurrentBag<Exception>();

        container.Registered += (sender, args) =>
        {
            Interlocked.Increment(ref eventCount);
        };

        // Act
        Parallel.For(0, 100, _ =>
        {
            try
            {
                container.Configure(c => c.Export<SimpleService>());
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        Assert.AreEqual(100, eventCount);
    }

    [TestMethod]
    public void ConcurrentUnregistration_EventsFire_NoExceptions()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var eventCount = 0;
        var exceptions = new ConcurrentBag<Exception>();

        container.Unregistered += (sender, args) =>
        {
            Interlocked.Increment(ref eventCount);
        };

        // Pre-register
        for (var i = 0; i < 100; i++)
        {
            container.Configure(c => c.Export<SimpleService>());
        }

        // Act
        Parallel.For(0, 100, _ =>
        {
            try
            {
                container.Unregister<SimpleService>();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.IsEmpty(exceptions, $"Exceptions occurred: {string.Join(", ", exceptions.Select(e => e.Message))}");
        // At least some unregistrations should have fired events (exact count depends on race conditions)
        Assert.IsGreaterThanOrEqualTo(1, eventCount);
    }

    #endregion
}

#region Test Helper Classes

public class ThreadSafeScopedService
{
    public Guid Id { get; } = Guid.NewGuid();
}

public class CountingService
{
    private static int _instanceCount;

    public static int InstanceCount => _instanceCount;

    public CountingService()
    {
        Interlocked.Increment(ref _instanceCount);
    }

    public static void ResetCount()
    {
        _instanceCount = 0;
    }
}

public class TrackingDisposableService : IDisposable
{
    public static Action? OnDispose { get; set; }

    public void Dispose()
    {
        OnDispose?.Invoke();
    }
}

#endregion
