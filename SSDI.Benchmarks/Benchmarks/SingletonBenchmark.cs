using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using Autofac;
using DryIoc;
using Grace.DependencyInjection;
using SimpleInjector;
using SSDI.Benchmarks.Classes;

namespace SSDI.Benchmarks;

/// <summary>
/// Benchmarks for resolving singleton services.
/// Tests singleton lookup performance.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SingletonBenchmark
{
    private DependencyInjectionContainer _ssdi = null!;
    private IServiceProvider _msdi = null!;
    private Autofac.IContainer _autofac = null!;
    private DryIoc.Container _dryioc = null!;
    private Grace.DependencyInjection.DependencyInjectionContainer _grace = null!;
    private SimpleInjector.Container _simpleInjector = null!;

    [GlobalSetup]
    public void Setup()
    {
        // SSDI
        _ssdi = new DependencyInjectionContainer();
        _ssdi.Configure(c =>
        {
            c.Export<Singleton>().As<ISingleton>().Lifestyle.Singleton();
        });

        // Microsoft.Extensions.DependencyInjection
        var services = new ServiceCollection();
        services.AddSingleton<ISingleton, Singleton>();
        _msdi = services.BuildServiceProvider();

        // Autofac
        var builder = new ContainerBuilder();
        builder.RegisterType<Singleton>().As<ISingleton>().SingleInstance();
        _autofac = builder.Build();

        // DryIoc
        _dryioc = new DryIoc.Container();
        _dryioc.Register<ISingleton, Singleton>(Reuse.Singleton);

        // Grace
        _grace = new Grace.DependencyInjection.DependencyInjectionContainer();
        _grace.Configure(c =>
        {
            c.Export<Singleton>().As<ISingleton>().Lifestyle.Singleton();
        });

        // SimpleInjector
        _simpleInjector = new SimpleInjector.Container();
        _simpleInjector.RegisterSingleton<ISingleton, Singleton>();
    }

    [Benchmark(Description = "SSDI")]
    public ISingleton SSDI_Singleton() => _ssdi.Locate<ISingleton>();

    [Benchmark(Description = "MS.DI")]
    public ISingleton MSDI_Singleton() => _msdi.GetRequiredService<ISingleton>();

    [Benchmark(Description = "Autofac")]
    public ISingleton Autofac_Singleton() => _autofac.Resolve<ISingleton>();

    [Benchmark(Description = "DryIoc")]
    public ISingleton DryIoc_Singleton() => _dryioc.Resolve<ISingleton>();

    [Benchmark(Description = "Grace")]
    public ISingleton Grace_Singleton() => _grace.Locate<ISingleton>();

    [Benchmark(Description = "SimpleInj")]
    public ISingleton SimpleInjector_Singleton() => _simpleInjector.GetInstance<ISingleton>();

    [GlobalCleanup]
    public void Cleanup()
    {
        (_msdi as IDisposable)?.Dispose();
        _autofac.Dispose();
        _dryioc.Dispose();
        _simpleInjector.Dispose();
    }
}
