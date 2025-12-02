using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SSDI.Tests;

[TestClass]
public class ScopedLifetimeTests
{
    #region Basic Scoped Resolution

    [TestMethod]
    public void Scoped_SameScope_ReturnsSameInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>().Lifestyle.Scoped());

        using var scope = container.CreateScope();

        // Act
        var instance1 = scope.Locate<SimpleService>();
        var instance2 = scope.Locate<SimpleService>();

        // Assert
        Assert.AreSame(instance1, instance2);
    }

    [TestMethod]
    public void Scoped_DifferentScopes_ReturnsDifferentInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>().Lifestyle.Scoped());

        using var scope1 = container.CreateScope();
        using var scope2 = container.CreateScope();

        // Act
        var instance1 = scope1.Locate<SimpleService>();
        var instance2 = scope2.Locate<SimpleService>();

        // Assert
        Assert.AreNotSame(instance1, instance2);
    }

    [TestMethod]
    public void Scoped_WithoutScope_ThrowsException()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>().Lifestyle.Scoped());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => container.Locate<SimpleService>());
    }

    [TestMethod]
    public void Scoped_WithInterface_ReturnsCorrectInstance()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceImplementation>().As<IService>().Lifestyle.Scoped());

        using var scope = container.CreateScope();

        // Act
        var instance1 = scope.Locate<IService>();
        var instance2 = scope.Locate<IService>();

        // Assert
        Assert.AreSame(instance1, instance2);
        Assert.IsInstanceOfType(instance1, typeof(ServiceImplementation));
    }

    #endregion

    #region Scope Disposal

    [TestMethod]
    public void Scope_Dispose_DisposesScopedInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<DisposableService>().Lifestyle.Scoped());

        DisposableService instance;
        using (var scope = container.CreateScope())
        {
            instance = scope.Locate<DisposableService>();
            Assert.IsFalse(instance.IsDisposed);
        }

        // Assert
        Assert.IsTrue(instance.IsDisposed);
    }

    [TestMethod]
    public async Task Scope_DisposeAsync_DisposesAsyncDisposableInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<AsyncDisposableService>().Lifestyle.Scoped());

        AsyncDisposableService instance;
        await using (var scope = container.CreateScope())
        {
            instance = scope.Locate<AsyncDisposableService>();
            Assert.IsFalse(instance.IsDisposed);
        }

        // Assert
        Assert.IsTrue(instance.IsDisposed);
    }

    [TestMethod]
    public void Scope_IsDisposed_ReturnsTrueAfterDispose()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        var scope = container.CreateScope();

        // Act
        Assert.IsFalse(scope.IsDisposed);
        scope.Dispose();

        // Assert
        Assert.IsTrue(scope.IsDisposed);
    }

    [TestMethod]
    public void Scope_LocateAfterDispose_ThrowsException()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<SimpleService>().Lifestyle.Scoped());

        var scope = container.CreateScope();
        scope.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => scope.Locate<SimpleService>());
    }

    #endregion

    #region Scoped with Dependencies

    [TestMethod]
    public void Scoped_WithScopedDependency_SharesDependencyWithinScope()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Scoped();
            c.Export<ServiceWithDependency>().Lifestyle.Scoped();
        });

        using var scope = container.CreateScope();

        // Act
        var service = scope.Locate<ServiceWithDependency>();
        var dependency = scope.Locate<SimpleService>();

        // Assert
        Assert.AreSame(dependency, service.Dependency);
    }

    [TestMethod]
    public void Scoped_WithSingletonDependency_SharesSingletonAcrossScopes()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<ServiceWithDependency>().Lifestyle.Scoped();
        });

        using var scope1 = container.CreateScope();
        using var scope2 = container.CreateScope();

        // Act
        var service1 = scope1.Locate<ServiceWithDependency>();
        var service2 = scope2.Locate<ServiceWithDependency>();

        // Assert - Different scoped services but same singleton dependency
        Assert.AreNotSame(service1, service2);
        Assert.AreSame(service1.Dependency, service2.Dependency);
    }

    [TestMethod]
    public void Scoped_WithTransientDependency_CreatesNewDependencyEachTime()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Transient();
            c.Export<ServiceWithDependency>().Lifestyle.Scoped();
        });

        using var scope = container.CreateScope();

        // Act
        var service = scope.Locate<ServiceWithDependency>();
        var directDependency = scope.Locate<SimpleService>();

        // Assert - Transient is always new, even within same scope
        Assert.AreNotSame(directDependency, service.Dependency);
    }

    #endregion

    #region Scoped with Parameters

    [TestMethod]
    public void Scoped_WithConstructorParameters_UsesParameters()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
            c.Export<ServiceWithParameters>()
                .WithCtorParam("name", "ScopedService")
                .WithCtorParam("port", 8080)
                .Lifestyle.Scoped());

        using var scope = container.CreateScope();

        // Act
        var instance = scope.Locate<ServiceWithParameters>();

        // Assert
        Assert.AreEqual("ScopedService", instance.Name);
        Assert.AreEqual(8080, instance.Port);
    }

    [TestMethod]
    public void Scope_LocateWithParameters_UsesRuntimeParameters()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c => c.Export<ServiceWithParameters>().Lifestyle.Scoped());

        using var scope = container.CreateScope();

        // Act
        var instance = scope.LocateWithNamedParameters<ServiceWithParameters>(
            ("name", "RuntimeName"),
            ("port", 9090));

        // Assert
        Assert.AreEqual("RuntimeName", instance.Name);
        Assert.AreEqual(9090, instance.Port);
    }

    #endregion

    #region Mixed Lifestyles

    [TestMethod]
    public void MixedLifestyles_AllWorkCorrectly()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<SimpleService>().Lifestyle.Singleton();
            c.Export<AnotherService>().Lifestyle.Scoped();
            c.Export<ServiceWithMultipleDependencies>().Lifestyle.Transient();
        });

        using var scope1 = container.CreateScope();
        using var scope2 = container.CreateScope();

        // Act
        var multi1a = scope1.Locate<ServiceWithMultipleDependencies>();
        var multi1b = scope1.Locate<ServiceWithMultipleDependencies>();
        var multi2 = scope2.Locate<ServiceWithMultipleDependencies>();

        // Assert
        // Singleton - same across all
        Assert.AreSame(multi1a.Service1, multi1b.Service1);
        Assert.AreSame(multi1a.Service1, multi2.Service1);

        // Scoped - same within scope, different across scopes
        Assert.AreSame(multi1a.Service2, multi1b.Service2);
        Assert.AreNotSame(multi1a.Service2, multi2.Service2);

        // Transient - always different
        Assert.AreNotSame(multi1a, multi1b);
        Assert.AreNotSame(multi1a, multi2);
    }

    #endregion

    #region IEnumerable Resolution

    [TestMethod]
    public void Scoped_IEnumerable_ReturnsScopedInstances()
    {
        // Arrange
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>().Lifestyle.Scoped();
            c.Export<GamePacketHandler>().As<IPacketHandler>().Lifestyle.Scoped();
        });

        using var scope = container.CreateScope();

        // Act
        var handlers1 = scope.Locate<IEnumerable<IPacketHandler>>().ToList();
        var handlers2 = scope.Locate<IEnumerable<IPacketHandler>>().ToList();

        // Assert
        Assert.HasCount(2, handlers1);
        Assert.HasCount(2, handlers2);

        // Same instances within scope
        var auth1 = handlers1.First(h => h is AuthPacketHandler);
        var auth2 = handlers2.First(h => h is AuthPacketHandler);
        Assert.AreSame(auth1, auth2);
    }

    #endregion

    #region Real-world Scenarios

    [TestMethod]
    public void PlayerScope_SimulatesRealUsage()
    {
        // Arrange - Register player-scoped services
        var container = new DependencyInjectionContainer();
        container.Configure(c =>
        {
            c.Export<PlayerInventory>().As<IInventory>().Lifestyle.Scoped();
            c.Export<PlayerStats>().Lifestyle.Scoped();
        });

        // Simulate two players connecting
        var player1Scope = container.CreateScope();
        var player2Scope = container.CreateScope();

        try
        {
            // Player 1 gets their inventory
            var p1Inventory1 = player1Scope.Locate<IInventory>();
            var p1Inventory2 = player1Scope.Locate<IInventory>();

            // Player 2 gets their inventory
            var p2Inventory = player2Scope.Locate<IInventory>();

            // Assert - same inventory for same player, different for different players
            Assert.AreSame(p1Inventory1, p1Inventory2);
            Assert.AreNotSame(p1Inventory1, p2Inventory);
        }
        finally
        {
            // Players disconnect
            player1Scope.Dispose();
            player2Scope.Dispose();
        }
    }

    #endregion
}

// Additional test helper classes for scoped tests
public class PlayerInventory : IInventory
{
    public List<string> Items { get; } = new();
}

public interface IInventory
{
    List<string> Items { get; }
}

public class PlayerStats
{
    public int Health { get; set; } = 100;
    public int Mana { get; set; } = 50;
}
