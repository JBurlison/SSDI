using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace SSDI.Tests;

[TestClass]
public class OpenGenericRegistrationTests
{
    [TestMethod]
    public void Export_OpenGeneric_AsOpenGeneric_Singleton_IsPerClosedGenericType()
    {
        var container = new DependencyInjectionContainer();

        container.Configure(c =>
            c.Export(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .Lifestyle.Singleton());

        var loggerInt1 = container.Locate<ILogger<int>>();
        var loggerInt2 = container.Locate<ILogger<int>>();
        var loggerString1 = container.Locate<ILogger<string>>();
        var loggerString2 = container.Locate<ILogger<string>>();

        Assert.AreSame(loggerInt1, loggerInt2);
        Assert.AreSame(loggerString1, loggerString2);
        Assert.AreNotEqual(loggerInt1.Id, loggerString1.Id);
    }

    [TestMethod]
    public void Export_OpenGeneric_AsOpenGeneric_Singleton_AliasAndConcreteShareInstance()
    {
        var container = new DependencyInjectionContainer();

        container.Configure(c =>
            c.Export(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .Lifestyle.Singleton());

        var asAlias = container.Locate<ILogger<int>>();
        var asConcrete = container.Locate<Logger<int>>();

        Assert.AreSame(asAlias, asConcrete);
    }

    [TestMethod]
    public void IEnumerable_WithOpenGenericAlias_ReturnsImplementation()
    {
        var container = new DependencyInjectionContainer();

        container.Configure(c =>
            c.Export(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .Lifestyle.Singleton());

        var list = container.Locate<IEnumerable<ILogger<int>>>().ToList();

        Assert.HasCount(1, list);
        Assert.IsInstanceOfType(list[0], typeof(Logger<int>));
    }

    [TestMethod]
    public void Export_OpenGeneric_AsOpenGeneric_AllowsComplexGenericArgument()
    {
        var container = new DependencyInjectionContainer();

        container.Configure(c =>
            c.Export(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .Lifestyle.Singleton());

        var logger1 = container.Locate<ILogger<ServiceWithMultipleDependencies>>();
        var logger2 = container.Locate<ILogger<ServiceWithMultipleDependencies>>();

        Assert.AreSame(logger1, logger2);
        Assert.IsInstanceOfType(logger1, typeof(Logger<ServiceWithMultipleDependencies>));
    }

    [TestMethod]
    public void Service_CanInjectOwnLogger_WithComplexDependencies()
    {
        var container = new DependencyInjectionContainer();

        container.Configure(c =>
        {
            c.Export(typeof(Logger<>)).As(typeof(ILogger<>)).Lifestyle.Singleton();

            c.Export<SimpleService>();
            c.Export<AnotherService>();
            c.Export<ServiceWithMultipleDependencies>();
            c.Export<ServiceWithOwnLogger>();
        });

        var instance1 = container.Locate<ServiceWithOwnLogger>();
        var instance2 = container.Locate<ServiceWithOwnLogger>();

        Assert.IsNotNull(instance1.Dependency);
        Assert.IsNotNull(instance1.Logger);
        Assert.IsNotNull(instance2.Logger);

        // Logger is singleton per closed generic type
        Assert.AreEqual(instance1.Logger.Id, instance2.Logger.Id);

        // Alias and concrete share the same singleton instance
        var resolvedLogger = container.Locate<ILogger<ServiceWithOwnLogger>>();
        Assert.AreEqual(instance1.Logger.Id, resolvedLogger.Id);
    }

    [TestMethod]
    public void NestedServices_CanInjectOwnLoggers_SingletonsArePerClosedType()
    {
        var container = new DependencyInjectionContainer();

        container.Configure(c =>
        {
            c.Export(typeof(Logger<>)).As(typeof(ILogger<>)).Lifestyle.Singleton();

            c.Export<ChildServiceWithOwnLogger>();
            c.Export<ParentServiceWithOwnLogger>();
        });

        var parent1 = container.Locate<ParentServiceWithOwnLogger>();
        var parent2 = container.Locate<ParentServiceWithOwnLogger>();

        Assert.IsNotNull(parent1.Child);
        Assert.IsNotNull(parent1.Logger);
        Assert.IsNotNull(parent1.Child.Logger);

        // Parent logger is stable across resolutions
        Assert.AreEqual(parent1.Logger.Id, parent2.Logger.Id);

        // Child logger is stable across resolutions
        Assert.AreEqual(parent1.Child.Logger.Id, parent2.Child.Logger.Id);

        // Different closed generic types should have different singleton instances
        Assert.AreNotEqual(parent1.Logger.Id, parent1.Child.Logger.Id);
    }
}
