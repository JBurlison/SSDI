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
/// Benchmarks for resolving simple transient services (10 dummy classes).
/// This tests the raw resolution speed without complex dependency graphs.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TransientBenchmark
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
            c.Export<Dummy1>().As<IDummy1>();
            c.Export<Dummy2>().As<IDummy2>();
            c.Export<Dummy3>().As<IDummy3>();
            c.Export<Dummy4>().As<IDummy4>();
            c.Export<Dummy5>().As<IDummy5>();
            c.Export<Dummy6>().As<IDummy6>();
            c.Export<Dummy7>().As<IDummy7>();
            c.Export<Dummy8>().As<IDummy8>();
            c.Export<Dummy9>().As<IDummy9>();
            c.Export<Dummy10>().As<IDummy10>();
        });

        // Microsoft.Extensions.DependencyInjection
        var services = new ServiceCollection();
        services.AddTransient<IDummy1, Dummy1>();
        services.AddTransient<IDummy2, Dummy2>();
        services.AddTransient<IDummy3, Dummy3>();
        services.AddTransient<IDummy4, Dummy4>();
        services.AddTransient<IDummy5, Dummy5>();
        services.AddTransient<IDummy6, Dummy6>();
        services.AddTransient<IDummy7, Dummy7>();
        services.AddTransient<IDummy8, Dummy8>();
        services.AddTransient<IDummy9, Dummy9>();
        services.AddTransient<IDummy10, Dummy10>();
        _msdi = services.BuildServiceProvider();

        // Autofac
        var builder = new ContainerBuilder();
        builder.RegisterType<Dummy1>().As<IDummy1>();
        builder.RegisterType<Dummy2>().As<IDummy2>();
        builder.RegisterType<Dummy3>().As<IDummy3>();
        builder.RegisterType<Dummy4>().As<IDummy4>();
        builder.RegisterType<Dummy5>().As<IDummy5>();
        builder.RegisterType<Dummy6>().As<IDummy6>();
        builder.RegisterType<Dummy7>().As<IDummy7>();
        builder.RegisterType<Dummy8>().As<IDummy8>();
        builder.RegisterType<Dummy9>().As<IDummy9>();
        builder.RegisterType<Dummy10>().As<IDummy10>();
        _autofac = builder.Build();

        // DryIoc
        _dryioc = new DryIoc.Container();
        _dryioc.Register<IDummy1, Dummy1>();
        _dryioc.Register<IDummy2, Dummy2>();
        _dryioc.Register<IDummy3, Dummy3>();
        _dryioc.Register<IDummy4, Dummy4>();
        _dryioc.Register<IDummy5, Dummy5>();
        _dryioc.Register<IDummy6, Dummy6>();
        _dryioc.Register<IDummy7, Dummy7>();
        _dryioc.Register<IDummy8, Dummy8>();
        _dryioc.Register<IDummy9, Dummy9>();
        _dryioc.Register<IDummy10, Dummy10>();

        // Grace
        _grace = new Grace.DependencyInjection.DependencyInjectionContainer();
        _grace.Configure(c =>
        {
            c.Export<Dummy1>().As<IDummy1>();
            c.Export<Dummy2>().As<IDummy2>();
            c.Export<Dummy3>().As<IDummy3>();
            c.Export<Dummy4>().As<IDummy4>();
            c.Export<Dummy5>().As<IDummy5>();
            c.Export<Dummy6>().As<IDummy6>();
            c.Export<Dummy7>().As<IDummy7>();
            c.Export<Dummy8>().As<IDummy8>();
            c.Export<Dummy9>().As<IDummy9>();
            c.Export<Dummy10>().As<IDummy10>();
        });

        // SimpleInjector
        _simpleInjector = new SimpleInjector.Container();
        _simpleInjector.Register<IDummy1, Dummy1>();
        _simpleInjector.Register<IDummy2, Dummy2>();
        _simpleInjector.Register<IDummy3, Dummy3>();
        _simpleInjector.Register<IDummy4, Dummy4>();
        _simpleInjector.Register<IDummy5, Dummy5>();
        _simpleInjector.Register<IDummy6, Dummy6>();
        _simpleInjector.Register<IDummy7, Dummy7>();
        _simpleInjector.Register<IDummy8, Dummy8>();
        _simpleInjector.Register<IDummy9, Dummy9>();
        _simpleInjector.Register<IDummy10, Dummy10>();
    }

    [Benchmark(Description = "SSDI")]
    public void SSDI_Transient()
    {
        _ssdi.Locate<IDummy1>();
        _ssdi.Locate<IDummy2>();
        _ssdi.Locate<IDummy3>();
        _ssdi.Locate<IDummy4>();
        _ssdi.Locate<IDummy5>();
        _ssdi.Locate<IDummy6>();
        _ssdi.Locate<IDummy7>();
        _ssdi.Locate<IDummy8>();
        _ssdi.Locate<IDummy9>();
        _ssdi.Locate<IDummy10>();
    }

    [Benchmark(Description = "MS.DI")]
    public void MSDI_Transient()
    {
        _msdi.GetService<IDummy1>();
        _msdi.GetService<IDummy2>();
        _msdi.GetService<IDummy3>();
        _msdi.GetService<IDummy4>();
        _msdi.GetService<IDummy5>();
        _msdi.GetService<IDummy6>();
        _msdi.GetService<IDummy7>();
        _msdi.GetService<IDummy8>();
        _msdi.GetService<IDummy9>();
        _msdi.GetService<IDummy10>();
    }

    [Benchmark(Description = "Autofac")]
    public void Autofac_Transient()
    {
        _autofac.Resolve<IDummy1>();
        _autofac.Resolve<IDummy2>();
        _autofac.Resolve<IDummy3>();
        _autofac.Resolve<IDummy4>();
        _autofac.Resolve<IDummy5>();
        _autofac.Resolve<IDummy6>();
        _autofac.Resolve<IDummy7>();
        _autofac.Resolve<IDummy8>();
        _autofac.Resolve<IDummy9>();
        _autofac.Resolve<IDummy10>();
    }

    [Benchmark(Description = "DryIoc")]
    public void DryIoc_Transient()
    {
        _dryioc.Resolve<IDummy1>();
        _dryioc.Resolve<IDummy2>();
        _dryioc.Resolve<IDummy3>();
        _dryioc.Resolve<IDummy4>();
        _dryioc.Resolve<IDummy5>();
        _dryioc.Resolve<IDummy6>();
        _dryioc.Resolve<IDummy7>();
        _dryioc.Resolve<IDummy8>();
        _dryioc.Resolve<IDummy9>();
        _dryioc.Resolve<IDummy10>();
    }

    [Benchmark(Description = "Grace")]
    public void Grace_Transient()
    {
        _grace.Locate<IDummy1>();
        _grace.Locate<IDummy2>();
        _grace.Locate<IDummy3>();
        _grace.Locate<IDummy4>();
        _grace.Locate<IDummy5>();
        _grace.Locate<IDummy6>();
        _grace.Locate<IDummy7>();
        _grace.Locate<IDummy8>();
        _grace.Locate<IDummy9>();
        _grace.Locate<IDummy10>();
    }

    [Benchmark(Description = "SimpleInj")]
    public void SimpleInjector_Transient()
    {
        _simpleInjector.GetInstance<IDummy1>();
        _simpleInjector.GetInstance<IDummy2>();
        _simpleInjector.GetInstance<IDummy3>();
        _simpleInjector.GetInstance<IDummy4>();
        _simpleInjector.GetInstance<IDummy5>();
        _simpleInjector.GetInstance<IDummy6>();
        _simpleInjector.GetInstance<IDummy7>();
        _simpleInjector.GetInstance<IDummy8>();
        _simpleInjector.GetInstance<IDummy9>();
        _simpleInjector.GetInstance<IDummy10>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_msdi as IDisposable)?.Dispose();
        _autofac.Dispose();
        _dryioc.Dispose();
        _simpleInjector.Dispose();
    }
}
