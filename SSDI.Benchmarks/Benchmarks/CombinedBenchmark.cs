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
/// Benchmarks for resolving combined services (singleton + transient dependencies).
/// Tests real-world usage patterns.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CombinedBenchmark
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
            c.Export<Transient>().As<ITransient>();
            c.Export<Combined>().As<ICombined>();
        });

        // Microsoft.Extensions.DependencyInjection
        var services = new ServiceCollection();
        services.AddSingleton<ISingleton, Singleton>();
        services.AddTransient<ITransient, Transient>();
        services.AddTransient<ICombined, Combined>();
        _msdi = services.BuildServiceProvider();

        // Autofac
        var builder = new ContainerBuilder();
        builder.RegisterType<Singleton>().As<ISingleton>().SingleInstance();
        builder.RegisterType<Transient>().As<ITransient>();
        builder.RegisterType<Combined>().As<ICombined>();
        _autofac = builder.Build();

        // DryIoc
        _dryioc = new DryIoc.Container();
        _dryioc.Register<ISingleton, Singleton>(Reuse.Singleton);
        _dryioc.Register<ITransient, Transient>();
        _dryioc.Register<ICombined, Combined>();

        // Grace
        _grace = new Grace.DependencyInjection.DependencyInjectionContainer();
        _grace.Configure(c =>
        {
            c.Export<Singleton>().As<ISingleton>().Lifestyle.Singleton();
            c.Export<Transient>().As<ITransient>();
            c.Export<Combined>().As<ICombined>();
        });

        // SimpleInjector
        _simpleInjector = new SimpleInjector.Container();
        _simpleInjector.RegisterSingleton<ISingleton, Singleton>();
        _simpleInjector.Register<ITransient, Transient>();
        _simpleInjector.Register<ICombined, Combined>();
    }

    [Benchmark(Description = "SSDI")]
    public ICombined SSDI_Combined() => _ssdi.Locate<ICombined>();

    [Benchmark(Description = "MS.DI")]
    public ICombined MSDI_Combined() => _msdi.GetRequiredService<ICombined>();

    [Benchmark(Description = "Autofac")]
    public ICombined Autofac_Combined() => _autofac.Resolve<ICombined>();

    [Benchmark(Description = "DryIoc")]
    public ICombined DryIoc_Combined() => _dryioc.Resolve<ICombined>();

    [Benchmark(Description = "Grace")]
    public ICombined Grace_Combined() => _grace.Locate<ICombined>();

    [Benchmark(Description = "SimpleInj")]
    public ICombined SimpleInjector_Combined() => _simpleInjector.GetInstance<ICombined>();

    [GlobalCleanup]
    public void Cleanup()
    {
        (_msdi as IDisposable)?.Dispose();
        _autofac.Dispose();
        _dryioc.Dispose();
        _simpleInjector.Dispose();
    }
}
