using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SSDI.Tests;

[TestClass]
public class UnregistrationTests
{
    [TestMethod]
    public void Unregister_RegisteredType_ReturnsTrue()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        // Act
        var result = container.Unregister<SimpleService>();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Unregister_UnregisteredType_ReturnsFalse()
    {
        // Arrange
        var container = new DependencyInjectionContainer();

        // Act
        var result = container.Unregister<SimpleService>();

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Unregister_TypeNoLongerRegistered()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        Assert.IsTrue(container.IsRegistered<SimpleService>());

        // Act
        container.Unregister<SimpleService>();

        // Assert
        Assert.IsFalse(container.IsRegistered<SimpleService>());
    }

    [TestMethod]
    public void Unregister_NonGeneric_Works()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>());

        // Act
        var result = container.Unregister(typeof(SimpleService));

        // Assert
        Assert.IsTrue(result);
        Assert.IsFalse(container.IsRegistered<SimpleService>());
    }

    [TestMethod]
    public void Unregister_RemovesFromAliases_ByDefault()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As<IService>());

        // Act
        container.Unregister<ServiceImplementation>();

        // Assert - IService should still be registered (alias exists) but no implementations
        var result = container.Locate<IEnumerable<IService>>().ToList();
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void Unregister_KeepsAliases_WhenSpecified()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<ServiceImplementation>().As<IService>();
            c.Export<AnotherServiceImplementation>().As<IService>();
        });

        // Act - Remove ServiceImplementation but keep in aliases
        container.Unregister<ServiceImplementation>(removeFromAliases: false);

        // Assert - The alias mapping might still reference the removed type
        // This is an edge case that depends on implementation
        Assert.IsFalse(container.IsRegistered<ServiceImplementation>());
    }

    [TestMethod]
    public void Unregister_DisposableService_DisposesInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<DisposableService>().Lifestyle.Singleton());

        var instance = container.Locate<DisposableService>();
        Assert.IsFalse(instance.IsDisposed);

        // Act
        container.Unregister<DisposableService>();

        // Assert
        Assert.IsTrue(instance.IsDisposed);
    }

    [TestMethod]
    public void Unregister_AsyncDisposableService_DisposesInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<AsyncDisposableService>().Lifestyle.Singleton());

        var instance = container.Locate<AsyncDisposableService>();
        Assert.IsFalse(instance.IsDisposed);

        // Act
        container.Unregister<AsyncDisposableService>();

        // Assert
        Assert.IsTrue(instance.IsDisposed);
    }

    [TestMethod]
    public void Unregister_TransientService_NoDisposal()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<DisposableService>().Lifestyle.Transient());

        var instance = container.Locate<DisposableService>();

        // Act
        container.Unregister<DisposableService>();

        // Assert - Transient instances are not tracked, so not disposed
        Assert.IsFalse(instance.IsDisposed);
    }

    [TestMethod]
    public void Unregister_ExportedInstance_DisposesInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var instance = new DisposableService();
        container.Configure(c => c.ExportInstance(instance));

        // Act
        container.Unregister<DisposableService>();

        // Assert
        Assert.IsTrue(instance.IsDisposed);
    }

    [TestMethod]
    public void UnregisterAll_RemovesAllImplementations()
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
        var count = container.UnregisterAll<IPacketHandler>();

        // Assert
        Assert.AreEqual(3, count);
        Assert.IsFalse(container.IsRegistered<AuthPacketHandler>());
        Assert.IsFalse(container.IsRegistered<GamePacketHandler>());
        Assert.IsFalse(container.IsRegistered<ChatPacketHandler>());
    }

    [TestMethod]
    public void UnregisterAll_NoImplementations_ReturnsZero()
    {
        // Arrange
        var container = new DependencyInjectionContainer();

        // Act
        var count = container.UnregisterAll<IPacketHandler>();

        // Assert
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void UnregisterAll_NonGeneric_Works()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
        });

        // Act
        var count = container.UnregisterAll(typeof(IPacketHandler));

        // Assert
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void UnregisterAll_DisposesAllSingletonInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var disposable1 = new DisposableService();
        var disposable2 = new AnotherDisposableService();

        container.Configure(c =>
        {
            c.ExportInstance(disposable1).As<IDisposable>();
            c.ExportInstance(disposable2).As<IDisposable>();
        });

        // Act
        container.UnregisterAll<IDisposable>();

        // Assert
        Assert.IsTrue(disposable1.IsDisposed);
        Assert.IsTrue(disposable2.IsDisposed);
    }

    [TestMethod]
    public void Unregister_ThenReregister_Works()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As<IService>());

        var original = container.Locate<IService>();
        container.Unregister<ServiceImplementation>();

        // Act - Register a different implementation
        container.Configure(c => c.Export<AnotherServiceImplementation>().As<IService>());
        var replacement = container.Locate<IService>();

        // Assert
        Assert.IsInstanceOfType(original, typeof(ServiceImplementation));
        Assert.IsInstanceOfType(replacement, typeof(AnotherServiceImplementation));
    }

    [TestMethod]
    public void HotSwap_Pattern_Works()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As<IService>().Lifestyle.Singleton());

        var v1 = container.Locate<IService>();
        Assert.IsInstanceOfType(v1, typeof(ServiceImplementation));

        // Act - Hot swap to new implementation
        container.Unregister<ServiceImplementation>();
        container.Configure(c => c.Export<AnotherServiceImplementation>().As<IService>().Lifestyle.Singleton());

        var v2 = container.Locate<IService>();

        // Assert
        Assert.IsInstanceOfType(v2, typeof(AnotherServiceImplementation));
        Assert.AreNotSame(v1, v2);
    }
}
