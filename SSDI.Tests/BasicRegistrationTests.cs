using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SSDI.Tests;

[TestClass]
public class BasicRegistrationTests
{
    [TestMethod]
    public void Locate_RegisteredType_ReturnsInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        // Act
        var result = container.Locate<SimpleService>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SimpleService));
    }

    [TestMethod]
    public void Locate_UnregisteredType_ReturnsInstanceViaActivator()
    {
        // Arrange
        var container = new DependencyInjectionContainer();

        // Act
        var result = container.Locate<SimpleService>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SimpleService));
    }

    [TestMethod]
    public void Locate_TypeWithDependency_ResolvesDependency()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<ServiceWithDependency>();
        });

        // Act
        var result = container.Locate<ServiceWithDependency>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Dependency);
    }

    [TestMethod]
    public void Locate_TypeWithMultipleDependencies_ResolvesAllDependencies()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<AnotherService>();
            c.Export<ServiceWithMultipleDependencies>();
        });

        // Act
        var result = container.Locate<ServiceWithMultipleDependencies>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Service1);
        Assert.IsNotNull(result.Service2);
    }

    [TestMethod]
    public void Locate_NonGeneric_ReturnsInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        // Act
        var result = container.Locate(typeof(SimpleService));

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SimpleService));
    }

    [TestMethod]
    public void Configure_CanBeCalledMultipleTimes()
    {
        // Arrange
        var container = new DependencyInjectionContainer();

        // Act
        container.Configure(c => c.Export<SimpleService>());
        container.Configure(c => c.Export<AnotherService>());

        var service1 = container.Locate<SimpleService>();
        var service2 = container.Locate<AnotherService>();

        // Assert
        Assert.IsNotNull(service1);
        Assert.IsNotNull(service2);
    }

    [TestMethod]
    public void IsRegistered_RegisteredType_ReturnsTrue()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        // Act
        var result = container.IsRegistered<SimpleService>();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRegistered_UnregisteredType_ReturnsFalse()
    {
        // Arrange
        var container = new DependencyInjectionContainer();

        // Act
        var result = container.IsRegistered<SimpleService>();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRegistered_NonGeneric_ReturnsCorrectValue()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        // Act & Assert
        Assert.IsTrue(container.IsRegistered(typeof(SimpleService)));
        Assert.IsFalse(container.IsRegistered(typeof(AnotherService)));
    }

    [TestMethod]
    public void ExportInstance_RegistersPrebuiltInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var instance = new SimpleService();
        container.Configure(c => c.ExportInstance(instance));

        // Act
        var result = container.Locate<SimpleService>();

        // Assert
        Assert.AreSame(instance, result);
    }

    [TestMethod]
    public void Export_NonGeneric_RegistersType()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export(typeof(SimpleService)));

        // Act
        var result = container.Locate<SimpleService>();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(SimpleService));
    }
}
