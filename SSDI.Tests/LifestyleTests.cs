using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SSDI.Tests;

[TestClass]
public class LifestyleTests
{
    [TestMethod]
    public void Transient_DefaultLifestyle_CreatesNewInstanceEachTime()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        // Act
        var instance1 = container.Locate<SimpleService>();
        var instance2 = container.Locate<SimpleService>();

        // Assert
        Assert.AreNotSame(instance1, instance2);
    }

    [TestMethod]
    public void Transient_Explicit_CreatesNewInstanceEachTime()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>().Lifestyle.Transient());

        // Act
        var instance1 = container.Locate<SimpleService>();
        var instance2 = container.Locate<SimpleService>();

        // Assert
        Assert.AreNotSame(instance1, instance2);
    }

    [TestMethod]
    public void Singleton_ReturnsSameInstanceEachTime()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());

        // Act
        var instance1 = container.Locate<SimpleService>();
        var instance2 = container.Locate<SimpleService>();

        // Assert
        Assert.AreSame(instance1, instance2);
    }

    [TestMethod]
    public void Singleton_WithDependencies_ReturnsSameInstanceEachTime()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<ServiceWithDependency>().Lifestyle.Singleton();
        });

        // Act
        var instance1 = container.Locate<ServiceWithDependency>();
        var instance2 = container.Locate<ServiceWithDependency>();

        // Assert
        Assert.AreSame(instance1, instance2);
        Assert.AreSame(instance1.Dependency, instance2.Dependency);
    }

    [TestMethod]
    public void ExportInstance_AutomaticallySingleton()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var instance = new SimpleService();
        container.Configure(c => c.ExportInstance(instance));

        // Act
        var result1 = container.Locate<SimpleService>();
        var result2 = container.Locate<SimpleService>();

        // Assert
        Assert.AreSame(instance, result1);
        Assert.AreSame(instance, result2);
    }

    [TestMethod]
    public void Lifestyle_SettingTwice_ThrowsException()
    {
        // Arrange
        var container = new DependencyInjectionContainer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            container.Configure(c =>
                c.Export<SimpleService>()
                    .Lifestyle.Singleton()
                    .Lifestyle.Transient());
        });
    }

    [TestMethod]
    public void Singleton_WithTransientDependency_DependencyIsShared()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<ServiceWithDependency>().Lifestyle.Transient();
        });

        // Act
        var instance1 = container.Locate<ServiceWithDependency>();
        var instance2 = container.Locate<ServiceWithDependency>();

        // Assert - Different service instances, but same dependency (because dependency is singleton)
        Assert.AreNotSame(instance1, instance2);
        Assert.AreSame(instance1.Dependency, instance2.Dependency);
    }

    [TestMethod]
    public void MixedLifestyles_MultipleTypes_CorrectBehavior()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<AnotherService>().Lifestyle.Transient();
        });

        // Act
        var simple1 = container.Locate<SimpleService>();
        var simple2 = container.Locate<SimpleService>();
        var another1 = container.Locate<AnotherService>();
        var another2 = container.Locate<AnotherService>();

        // Assert
        Assert.AreSame(simple1, simple2);
        Assert.AreNotSame(another1, another2);
    }
}
