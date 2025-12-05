using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using SSDI.Benchmarks.Classes;

namespace SSDI.Benchmarks;

/// <summary>
/// Benchmarks for unregistration operations.
/// Tests how fast SSDI can unregister types with cascade invalidation.
/// Note: Only SSDI supports dynamic unregistration; other containers don't have this feature.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class UnregisterBenchmark
{
    /// <summary>
    /// Unregister a simple transient with no dependents
    /// </summary>
    [Benchmark(Description = "Simple Transient")]
    public bool Unregister_SimpleTransient()
    {
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<Dummy1>().As<IDummy1>();
        });
        // Pre-warm
        _ = container.Locate<IDummy1>();

        return container.Unregister<Dummy1>();
    }

    /// <summary>
    /// Unregister a singleton (includes disposal)
    /// </summary>
    [Benchmark(Description = "Singleton")]
    public bool Unregister_Singleton()
    {
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<Singleton>().As<ISingleton>().Lifestyle.Singleton();
        });
        // Pre-warm - create the singleton instance
        _ = container.Locate<ISingleton>();

        return container.Unregister<Singleton>();
    }

    /// <summary>
    /// Unregister a service with dependents (cascade invalidation)
    /// </summary>
    [Benchmark(Description = "With Dependents")]
    public bool Unregister_WithDependents()
    {
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<FirstService>().As<IFirstService>().Lifestyle.Singleton();
            c.Export<SecondService>().As<ISecondService>().Lifestyle.Singleton();
            c.Export<ThirdService>().As<IThirdService>().Lifestyle.Singleton();
            c.Export<SubObject1>().As<ISubObject1>();
            c.Export<SubObject2>().As<ISubObject2>();
            c.Export<SubObject3>().As<ISubObject3>();
            c.Export<Complex>().As<IComplex>();
        });
        // Pre-warm all dependencies
        _ = container.Locate<IComplex>();

        // Unregister a service that Complex depends on (triggers cascade invalidation)
        return container.Unregister<SubObject1>();
    }

    /// <summary>
    /// UnregisterAll - remove all implementations of an interface
    /// </summary>
    [Benchmark(Description = "UnregisterAll")]
    public int UnregisterAll_MultipleImplementations()
    {
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<Dummy1>().As<IDummy1>();
            c.Export<Dummy2>().As<IDummy1>(); // Second implementation
            c.Export<Dummy3>().As<IDummy1>(); // Third implementation
        });
        // Pre-warm
        _ = container.Locate<IEnumerable<IDummy1>>();

        return container.UnregisterAll<IDummy1>();
    }

    /// <summary>
    /// Re-register after unregister (hot-swap pattern)
    /// </summary>
    [Benchmark(Description = "Hot-Swap")]
    public IDummy1 HotSwap_UnregisterAndReregister()
    {
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<Dummy1>().As<IDummy1>();
        });
        // Pre-warm
        _ = container.Locate<IDummy1>();

        // Unregister old
        container.Unregister<Dummy1>();

        // Register new
        container.Configure(c =>
        {
            c.Export<Dummy2>().As<IDummy1>();
        });

        // Resolve new
        return container.Locate<IDummy1>();
    }
}
