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
/// Benchmarks for resolving complex services with deep dependency graphs.
/// Tests performance with many constructor parameters and nested dependencies.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ComplexBenchmark
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
            c.Export<FirstService>().As<IFirstService>().Lifestyle.Singleton();
            c.Export<SecondService>().As<ISecondService>().Lifestyle.Singleton();
            c.Export<ThirdService>().As<IThirdService>().Lifestyle.Singleton();
            c.Export<SubObject1>().As<ISubObject1>();
            c.Export<SubObject2>().As<ISubObject2>();
            c.Export<SubObject3>().As<ISubObject3>();
            c.Export<Complex>().As<IComplex>();
        });

        // Microsoft.Extensions.DependencyInjection
        var services = new ServiceCollection();
        services.AddSingleton<IFirstService, FirstService>();
        services.AddSingleton<ISecondService, SecondService>();
        services.AddSingleton<IThirdService, ThirdService>();
        services.AddTransient<ISubObject1, SubObject1>();
        services.AddTransient<ISubObject2, SubObject2>();
        services.AddTransient<ISubObject3, SubObject3>();
        services.AddTransient<IComplex, Complex>();
        _msdi = services.BuildServiceProvider();

        // Autofac
        var builder = new ContainerBuilder();
        builder.RegisterType<FirstService>().As<IFirstService>().SingleInstance();
        builder.RegisterType<SecondService>().As<ISecondService>().SingleInstance();
        builder.RegisterType<ThirdService>().As<IThirdService>().SingleInstance();
        builder.RegisterType<SubObject1>().As<ISubObject1>();
        builder.RegisterType<SubObject2>().As<ISubObject2>();
        builder.RegisterType<SubObject3>().As<ISubObject3>();
        builder.RegisterType<Complex>().As<IComplex>();
        _autofac = builder.Build();

        // DryIoc
        _dryioc = new DryIoc.Container();
        _dryioc.Register<IFirstService, FirstService>(Reuse.Singleton);
        _dryioc.Register<ISecondService, SecondService>(Reuse.Singleton);
        _dryioc.Register<IThirdService, ThirdService>(Reuse.Singleton);
        _dryioc.Register<ISubObject1, SubObject1>();
        _dryioc.Register<ISubObject2, SubObject2>();
        _dryioc.Register<ISubObject3, SubObject3>();
        _dryioc.Register<IComplex, Complex>();

        // Grace
        _grace = new Grace.DependencyInjection.DependencyInjectionContainer();
        _grace.Configure(c =>
        {
            c.Export<FirstService>().As<IFirstService>().Lifestyle.Singleton();
            c.Export<SecondService>().As<ISecondService>().Lifestyle.Singleton();
            c.Export<ThirdService>().As<IThirdService>().Lifestyle.Singleton();
            c.Export<SubObject1>().As<ISubObject1>();
            c.Export<SubObject2>().As<ISubObject2>();
            c.Export<SubObject3>().As<ISubObject3>();
            c.Export<Complex>().As<IComplex>();
        });

        // SimpleInjector
        _simpleInjector = new SimpleInjector.Container();
        _simpleInjector.RegisterSingleton<IFirstService, FirstService>();
        _simpleInjector.RegisterSingleton<ISecondService, SecondService>();
        _simpleInjector.RegisterSingleton<IThirdService, ThirdService>();
        _simpleInjector.Register<ISubObject1, SubObject1>();
        _simpleInjector.Register<ISubObject2, SubObject2>();
        _simpleInjector.Register<ISubObject3, SubObject3>();
        _simpleInjector.Register<IComplex, Complex>();
    }

    [Benchmark(Description = "SSDI")]
    public IComplex SSDI_Complex() => _ssdi.Locate<IComplex>();

    [Benchmark(Description = "MS.DI")]
    public IComplex MSDI_Complex() => _msdi.GetRequiredService<IComplex>();

    [Benchmark(Description = "Autofac")]
    public IComplex Autofac_Complex() => _autofac.Resolve<IComplex>();

    [Benchmark(Description = "DryIoc")]
    public IComplex DryIoc_Complex() => _dryioc.Resolve<IComplex>();

    [Benchmark(Description = "Grace")]
    public IComplex Grace_Complex() => _grace.Locate<IComplex>();

    [Benchmark(Description = "SimpleInj")]
    public IComplex SimpleInjector_Complex() => _simpleInjector.GetInstance<IComplex>();

    [GlobalCleanup]
    public void Cleanup()
    {
        (_msdi as IDisposable)?.Dispose();
        _autofac.Dispose();
        _dryioc.Dispose();
        _simpleInjector.Dispose();
    }
}
