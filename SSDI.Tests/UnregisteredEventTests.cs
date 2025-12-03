using SSDI.Events;

namespace SSDI.Tests;

[TestClass]
public class UnregisteredEventTests
{
    private DependencyInjectionContainer _container = null!;

    [TestInitialize]
    public void Setup()
    {
        _container = new DependencyInjectionContainer();
    }

    [TestMethod]
    public void Unregister_ShouldFireEvent()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>());
        
        UnregisteredEventArgs? eventArgs = null;
        _container.Unregistered += (sender, args) => eventArgs = args;

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(typeof(SimpleService), eventArgs.UnregisteredType);
    }

    [TestMethod]
    public void Unregister_ShouldNotFireEvent_WhenTypeNotRegistered()
    {
        // Arrange
        var eventFired = false;
        _container.Unregistered += (sender, args) => eventFired = true;

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.IsFalse(eventFired);
    }

    [TestMethod]
    public void Unregister_Singleton_ShouldIncludeInstanceInEvent()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());
        var originalInstance = _container.Locate<SimpleService>();
        
        UnregisteredEventArgs? eventArgs = null;
        _container.Unregistered += (sender, args) => eventArgs = args;

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreSame(originalInstance, eventArgs.Instance);
    }

    [TestMethod]
    public void Unregister_Transient_ShouldHaveNullInstance()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>().Lifestyle.Transient());
        _ = _container.Locate<SimpleService>(); // Create an instance (not cached)
        
        UnregisteredEventArgs? eventArgs = null;
        _container.Unregistered += (sender, args) => eventArgs = args;

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.IsNull(eventArgs.Instance);
    }

    [TestMethod]
    public void Unregister_DisposableSingleton_ShouldSetWasDisposed()
    {
        // Arrange
        _container.Configure(c => c.Export<DisposableService>().Lifestyle.Singleton());
        var instance = _container.Locate<DisposableService>();
        
        UnregisteredEventArgs? eventArgs = null;
        _container.Unregistered += (sender, args) => eventArgs = args;

        // Act
        _container.Unregister<DisposableService>();

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.IsTrue(eventArgs.WasDisposed);
        Assert.IsTrue(instance.IsDisposed);
    }

    [TestMethod]
    public void Unregister_NonDisposableSingleton_ShouldNotSetWasDisposed()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());
        _ = _container.Locate<SimpleService>();
        
        UnregisteredEventArgs? eventArgs = null;
        _container.Unregistered += (sender, args) => eventArgs = args;

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.IsFalse(eventArgs.WasDisposed);
    }

    [TestMethod]
    public void UnregisterAll_ShouldFireEventForEachType()
    {
        // Arrange
        _container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
            c.Export<ChatPacketHandler>().As<IPacketHandler>();
        });
        
        var unregisteredTypes = new List<Type>();
        _container.Unregistered += (sender, args) => unregisteredTypes.Add(args.UnregisteredType);

        // Act
        var count = _container.UnregisterAll<IPacketHandler>();

        // Assert
        Assert.AreEqual(3, count);
        Assert.AreEqual(3, unregisteredTypes.Count());
        CollectionAssert.Contains(unregisteredTypes, typeof(AuthPacketHandler));
        CollectionAssert.Contains(unregisteredTypes, typeof(GamePacketHandler));
        CollectionAssert.Contains(unregisteredTypes, typeof(ChatPacketHandler));
    }

    [TestMethod]
    public void Unregister_ShouldPassCorrectSender()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>());
        
        object? sender = null;
        _container.Unregistered += (s, args) => sender = s;

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.AreSame(_container, sender);
    }

    [TestMethod]
    public void Unregister_MultipleHandlers_ShouldFireAll()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>());
        
        var handler1Called = false;
        var handler2Called = false;
        _container.Unregistered += (sender, args) => handler1Called = true;
        _container.Unregistered += (sender, args) => handler2Called = true;

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.IsTrue(handler1Called);
        Assert.IsTrue(handler2Called);
    }

    [TestMethod]
    public void Unregister_CanUnsubscribeFromEvent()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>());
        _container.Configure(c => c.Export<AnotherService>());
        
        var callCount = 0;
        void Handler(object? sender, UnregisteredEventArgs args) => callCount++;
        
        _container.Unregistered += Handler;

        // Act
        _container.Unregister<SimpleService>();
        _container.Unregistered -= Handler;
        _container.Unregister<AnotherService>();

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public void Unregister_EventArgs_ShouldContainCorrectType()
    {
        // Arrange
        _container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<AnotherService>();
        });
        
        var unregisteredTypes = new List<Type>();
        _container.Unregistered += (sender, args) => unregisteredTypes.Add(args.UnregisteredType);

        // Act
        _container.Unregister<SimpleService>();
        _container.Unregister<AnotherService>();

        // Assert
        Assert.AreEqual(2, unregisteredTypes.Count());
        Assert.AreEqual(typeof(SimpleService), unregisteredTypes[0]);
        Assert.AreEqual(typeof(AnotherService), unregisteredTypes[1]);
    }
}
