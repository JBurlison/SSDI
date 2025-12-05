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
/// Benchmarks for container preparation/registration time.
/// Tests how fast each container can register types.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PrepareBenchmark
{
    [Benchmark(Description = "SSDI")]
    public DependencyInjectionContainer SSDI_Prepare()
    {
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            // Dummies
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
            // Standard
            c.Export<Singleton>().As<ISingleton>().Lifestyle.Singleton();
            c.Export<Transient>().As<ITransient>();
            c.Export<Combined>().As<ICombined>();
            // Complex
            c.Export<FirstService>().As<IFirstService>().Lifestyle.Singleton();
            c.Export<SecondService>().As<ISecondService>().Lifestyle.Singleton();
            c.Export<ThirdService>().As<IThirdService>().Lifestyle.Singleton();
            c.Export<SubObject1>().As<ISubObject1>();
            c.Export<SubObject2>().As<ISubObject2>();
            c.Export<SubObject3>().As<ISubObject3>();
            c.Export<Complex>().As<IComplex>();
        });
        return container;
    }

    [Benchmark(Description = "SSDI-Eager")]
    public DependencyInjectionContainer SSDI_Eager_Prepare()
    {
        var container = new DependencyInjectionContainer { EagerCompilation = true };
        container.Configure(c =>
        {
            // Dummies
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
            // Standard
            c.Export<Singleton>().As<ISingleton>().Lifestyle.Singleton();
            c.Export<Transient>().As<ITransient>();
            c.Export<Combined>().As<ICombined>();
            // Complex
            c.Export<FirstService>().As<IFirstService>().Lifestyle.Singleton();
            c.Export<SecondService>().As<ISecondService>().Lifestyle.Singleton();
            c.Export<ThirdService>().As<IThirdService>().Lifestyle.Singleton();
            c.Export<SubObject1>().As<ISubObject1>();
            c.Export<SubObject2>().As<ISubObject2>();
            c.Export<SubObject3>().As<ISubObject3>();
            c.Export<Complex>().As<IComplex>();
        });
        return container;
    }

    [Benchmark(Description = "MS.DI")]
    public IServiceProvider MSDI_Prepare()
    {
        var services = new ServiceCollection();
        // Dummies
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
        // Standard
        services.AddSingleton<ISingleton, Singleton>();
        services.AddTransient<ITransient, Transient>();
        services.AddTransient<ICombined, Combined>();
        // Complex
        services.AddSingleton<IFirstService, FirstService>();
        services.AddSingleton<ISecondService, SecondService>();
        services.AddSingleton<IThirdService, ThirdService>();
        services.AddTransient<ISubObject1, SubObject1>();
        services.AddTransient<ISubObject2, SubObject2>();
        services.AddTransient<ISubObject3, SubObject3>();
        services.AddTransient<IComplex, Complex>();
        return services.BuildServiceProvider();
    }

    [Benchmark(Description = "Autofac")]
    public Autofac.IContainer Autofac_Prepare()
    {
        var builder = new ContainerBuilder();
        // Dummies
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
        // Standard
        builder.RegisterType<Singleton>().As<ISingleton>().SingleInstance();
        builder.RegisterType<Transient>().As<ITransient>();
        builder.RegisterType<Combined>().As<ICombined>();
        // Complex
        builder.RegisterType<FirstService>().As<IFirstService>().SingleInstance();
        builder.RegisterType<SecondService>().As<ISecondService>().SingleInstance();
        builder.RegisterType<ThirdService>().As<IThirdService>().SingleInstance();
        builder.RegisterType<SubObject1>().As<ISubObject1>();
        builder.RegisterType<SubObject2>().As<ISubObject2>();
        builder.RegisterType<SubObject3>().As<ISubObject3>();
        builder.RegisterType<Complex>().As<IComplex>();
        return builder.Build();
    }

    [Benchmark(Description = "DryIoc")]
    public DryIoc.Container DryIoc_Prepare()
    {
        var container = new DryIoc.Container();
        // Dummies
        container.Register<IDummy1, Dummy1>();
        container.Register<IDummy2, Dummy2>();
        container.Register<IDummy3, Dummy3>();
        container.Register<IDummy4, Dummy4>();
        container.Register<IDummy5, Dummy5>();
        container.Register<IDummy6, Dummy6>();
        container.Register<IDummy7, Dummy7>();
        container.Register<IDummy8, Dummy8>();
        container.Register<IDummy9, Dummy9>();
        container.Register<IDummy10, Dummy10>();
        // Standard
        container.Register<ISingleton, Singleton>(Reuse.Singleton);
        container.Register<ITransient, Transient>();
        container.Register<ICombined, Combined>();
        // Complex
        container.Register<IFirstService, FirstService>(Reuse.Singleton);
        container.Register<ISecondService, SecondService>(Reuse.Singleton);
        container.Register<IThirdService, ThirdService>(Reuse.Singleton);
        container.Register<ISubObject1, SubObject1>();
        container.Register<ISubObject2, SubObject2>();
        container.Register<ISubObject3, SubObject3>();
        container.Register<IComplex, Complex>();
        return container;
    }

    [Benchmark(Description = "Grace")]
    public Grace.DependencyInjection.DependencyInjectionContainer Grace_Prepare()
    {
        var container = new Grace.DependencyInjection.DependencyInjectionContainer();
        container.Configure(c =>
        {
            // Dummies
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
            // Standard
            c.Export<Singleton>().As<ISingleton>().Lifestyle.Singleton();
            c.Export<Transient>().As<ITransient>();
            c.Export<Combined>().As<ICombined>();
            // Complex
            c.Export<FirstService>().As<IFirstService>().Lifestyle.Singleton();
            c.Export<SecondService>().As<ISecondService>().Lifestyle.Singleton();
            c.Export<ThirdService>().As<IThirdService>().Lifestyle.Singleton();
            c.Export<SubObject1>().As<ISubObject1>();
            c.Export<SubObject2>().As<ISubObject2>();
            c.Export<SubObject3>().As<ISubObject3>();
            c.Export<Complex>().As<IComplex>();
        });
        return container;
    }

    [Benchmark(Description = "SimpleInj")]
    public SimpleInjector.Container SimpleInjector_Prepare()
    {
        var container = new SimpleInjector.Container();
        // Dummies
        container.Register<IDummy1, Dummy1>();
        container.Register<IDummy2, Dummy2>();
        container.Register<IDummy3, Dummy3>();
        container.Register<IDummy4, Dummy4>();
        container.Register<IDummy5, Dummy5>();
        container.Register<IDummy6, Dummy6>();
        container.Register<IDummy7, Dummy7>();
        container.Register<IDummy8, Dummy8>();
        container.Register<IDummy9, Dummy9>();
        container.Register<IDummy10, Dummy10>();
        // Standard
        container.RegisterSingleton<ISingleton, Singleton>();
        container.Register<ITransient, Transient>();
        container.Register<ICombined, Combined>();
        // Complex
        container.RegisterSingleton<IFirstService, FirstService>();
        container.RegisterSingleton<ISecondService, SecondService>();
        container.RegisterSingleton<IThirdService, ThirdService>();
        container.Register<ISubObject1, SubObject1>();
        container.Register<ISubObject2, SubObject2>();
        container.Register<ISubObject3, SubObject3>();
        container.Register<IComplex, Complex>();
        return container;
    }
}
