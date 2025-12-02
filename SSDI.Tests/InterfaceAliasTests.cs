using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SSDI.Tests;

[TestClass]
public class InterfaceAliasTests
{
    [TestMethod]
    public void As_RegistersInterface_CanLocateByInterface()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As<IService>());

        // Act
        var result = container.Locate<IService>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ServiceImplementation));
    }

    [TestMethod]
    public void As_NonGeneric_RegistersInterface()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As(typeof(IService)));

        // Act
        var result = container.Locate<IService>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(ServiceImplementation));
    }

    [TestMethod]
    public void As_MultipleImplementations_LocateReturnsFirst()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<ServiceImplementation>().As<IService>();
            c.Export<AnotherServiceImplementation>().As<IService>();
        });

        // Act
        var result = container.Locate<IService>();

        // Assert
        Assert.IsNotNull(result);
        // Should return one of the implementations
        Assert.IsTrue(result is ServiceImplementation || result is AnotherServiceImplementation);
    }

    [TestMethod]
    public void IEnumerable_MultipleImplementations_ReturnsAll()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
            c.Export<ChatPacketHandler>().As<IPacketHandler>();
        });

        // Act
        var result = container.Locate<IEnumerable<IPacketHandler>>().ToList();

        // Assert
        Assert.HasCount(3, result);
        Assert.IsTrue(result.Any(h => h is AuthPacketHandler));
        Assert.IsTrue(result.Any(h => h is GamePacketHandler));
        Assert.IsTrue(result.Any(h => h is ChatPacketHandler));
    }

    [TestMethod]
    public void IEnumerable_NoImplementations_ReturnsEmptyList()
    {
        // Arrange
        var container = new DependencyInjectionContainer();

        // Act
        var result = container.Locate<IEnumerable<IPacketHandler>>().ToList();

        // Assert
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void IsRegistered_Interface_ReturnsTrueWhenAliasExists()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As<IService>());

        // Act & Assert
        Assert.IsTrue(container.IsRegistered<IService>());
    }

    [TestMethod]
    public void As_CanChainMultipleInterfaces()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceImplementation>()
                .As<IService>()
                .As<object>());

        // Act
        var asService = container.Locate<IService>();
        var asObject = container.Locate<object>();

        // Assert
        Assert.IsNotNull(asService);
        Assert.IsNotNull(asObject);
        Assert.IsInstanceOfType(asService, typeof(ServiceImplementation));
    }

    [TestMethod]
    public void As_WithSingleton_AllAliasesShareInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceImplementation>()
                .As<IService>()
                .Lifestyle.Singleton());

        // Act
        var instance1 = container.Locate<IService>();
        var instance2 = container.Locate<IService>();
        var instance3 = container.Locate<ServiceImplementation>();

        // Assert
        Assert.AreSame(instance1, instance2);
        Assert.AreSame(instance1, instance3);
    }

    [TestMethod]
    public void As_WithTransient_EachResolveCreatesNewInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceImplementation>()
                .As<IService>()
                .Lifestyle.Transient());

        // Act
        var instance1 = container.Locate<IService>();
        var instance2 = container.Locate<IService>();

        // Assert
        Assert.AreNotSame(instance1, instance2);
    }

    [TestMethod]
    public void IEnumerable_WithSingletons_ReturnsSameInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>().Lifestyle.Singleton();
            c.Export<GamePacketHandler>().As<IPacketHandler>().Lifestyle.Singleton();
        });

        // Act
        var result1 = container.Locate<IEnumerable<IPacketHandler>>().ToList();
        var result2 = container.Locate<IEnumerable<IPacketHandler>>().ToList();

        // Assert
        Assert.HasCount(2, result1);
        Assert.HasCount(2, result2);

        var auth1 = result1.First(h => h is AuthPacketHandler);
        var auth2 = result2.First(h => h is AuthPacketHandler);
        Assert.AreSame(auth1, auth2);
    }

    [TestMethod]
    public void Locate_InterfaceWithDependency_ResolvesDependency()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<ServiceWithDependency>().As<object>();
        });

        // Act
        var result = container.Locate<object>() as ServiceWithDependency;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Dependency);
    }
}
