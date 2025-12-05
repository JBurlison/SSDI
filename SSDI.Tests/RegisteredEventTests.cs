using SSDI.Events;
using SSDI.Registration;

namespace SSDI.Tests;

[TestClass]
public class RegisteredEventTests
{
    private DependencyInjectionContainer _container = null!;

    [TestInitialize]
    public void Setup()
    {
        _container = new DependencyInjectionContainer();
    }

    [TestMethod]
    public void Register_ShouldFireEvent()
    {
        // Arrange
        RegisteredEventArgs? eventArgs = null;
        _container.Registered += (sender, args) => eventArgs = args;

        // Act
        _container.Configure(c => c.Export<SimpleService>());

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(typeof(SimpleService), eventArgs.RegisteredType);
    }

    [TestMethod]
    public void Register_ShouldIncludeAliases()
    {
        // Arrange
        RegisteredEventArgs? eventArgs = null;
        _container.Registered += (sender, args) => eventArgs = args;

        // Act
        _container.Configure(c => c.Export<ServiceImplementation>().As<IService>());

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(typeof(ServiceImplementation), eventArgs.RegisteredType);
        Assert.AreEqual(1, eventArgs.Aliases.Count);
        Assert.AreEqual(typeof(IService), eventArgs.Aliases[0]);
    }

    [TestMethod]
    public void Register_ShouldIncludeLifestyle_Transient()
    {
        // Arrange
        RegisteredEventArgs? eventArgs = null;
        _container.Registered += (sender, args) => eventArgs = args;

        // Act
        _container.Configure(c => c.Export<SimpleService>().Lifestyle.Transient());

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(LifestyleType.Transient, eventArgs.Lifestyle);
        Assert.IsFalse(eventArgs.HasInstance);
    }

    [TestMethod]
    public void Register_ShouldIncludeLifestyle_Singleton()
    {
        // Arrange
        RegisteredEventArgs? eventArgs = null;
        _container.Registered += (sender, args) => eventArgs = args;

        // Act
        _container.Configure(c => c.Export<SimpleService>().Lifestyle.Singleton());

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(LifestyleType.Singleton, eventArgs.Lifestyle);
        Assert.IsFalse(eventArgs.HasInstance);
    }

    [TestMethod]
    public void Register_Instance_ShouldSetHasInstance()
    {
        // Arrange
        RegisteredEventArgs? eventArgs = null;
        _container.Registered += (sender, args) => eventArgs = args;
        var instance = new SimpleService();

        // Act
        _container.Configure(c => c.ExportInstance(instance));

        // Assert
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(typeof(SimpleService), eventArgs.RegisteredType);
        Assert.AreEqual(LifestyleType.Singleton, eventArgs.Lifestyle);
        Assert.IsTrue(eventArgs.HasInstance);
    }

    [TestMethod]
    public void Register_MultipleTypes_ShouldFireEventForEach()
    {
        // Arrange
        var registeredTypes = new List<Type>();
        _container.Registered += (sender, args) => registeredTypes.Add(args.RegisteredType);

        // Act
        _container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<AnotherService>();
            c.Export<DisposableService>();
        });

        // Assert
        Assert.AreEqual(3, registeredTypes.Count);
        CollectionAssert.Contains(registeredTypes, typeof(SimpleService));
        CollectionAssert.Contains(registeredTypes, typeof(AnotherService));
        CollectionAssert.Contains(registeredTypes, typeof(DisposableService));
    }

    [TestMethod]
    public void Register_ShouldPassCorrectSender()
    {
        // Arrange
        object? sender = null;
        _container.Registered += (s, args) => sender = s;

        // Act
        _container.Configure(c => c.Export<SimpleService>());

        // Assert
        Assert.AreSame(_container, sender);
    }

    [TestMethod]
    public void Register_MultipleHandlers_ShouldFireAll()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;
        _container.Registered += (sender, args) => handler1Called = true;
        _container.Registered += (sender, args) => handler2Called = true;

        // Act
        _container.Configure(c => c.Export<SimpleService>());

        // Assert
        Assert.IsTrue(handler1Called);
        Assert.IsTrue(handler2Called);
    }

    [TestMethod]
    public void Register_CanUnsubscribeFromEvent()
    {
        // Arrange
        var callCount = 0;
        void Handler(object? sender, RegisteredEventArgs args) => callCount++;

        _container.Registered += Handler;

        // Act
        _container.Configure(c => c.Export<SimpleService>());
        _container.Registered -= Handler;
        _container.Configure(c => c.Export<AnotherService>());

        // Assert
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task RegisteredAsync_ShouldFireAsyncEvent()
    {
        // Arrange
        RegisteredEventArgs? eventArgs = null;
        var tcs = new TaskCompletionSource<bool>();
        _container.RegisteredAsync += async (sender, args) =>
        {
            await Task.Yield();
            eventArgs = args;
            tcs.SetResult(true);
        };

        // Act
        _container.Configure(c => c.Export<SimpleService>());

        // Assert - wait for async handler to complete (fire-and-forget style)
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(typeof(SimpleService), eventArgs.RegisteredType);
    }

    [TestMethod]
    public async Task RegisteredAsync_ShouldFireBothSyncAndAsyncEvents()
    {
        // Arrange
        var syncHandlerCalled = false;
        var tcs = new TaskCompletionSource<bool>();

        _container.Registered += (sender, args) => syncHandlerCalled = true;
        _container.RegisteredAsync += async (sender, args) =>
        {
            await Task.Yield();
            tcs.SetResult(true);
        };

        // Act
        _container.Configure(c => c.Export<SimpleService>());

        // Assert
        Assert.IsTrue(syncHandlerCalled);
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
    }

    [TestMethod]
    public async Task RegisteredAsync_MultipleAsyncHandlers_ShouldFireAll()
    {
        // Arrange
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();

        _container.RegisteredAsync += async (sender, args) =>
        {
            await Task.Yield();
            tcs1.SetResult(true);
        };
        _container.RegisteredAsync += async (sender, args) =>
        {
            await Task.Yield();
            tcs2.SetResult(true);
        };

        // Act
        _container.Configure(c => c.Export<SimpleService>());

        // Assert
        await Task.WhenAny(Task.WhenAll(tcs1.Task, tcs2.Task), Task.Delay(1000));
        Assert.IsTrue(tcs1.Task.IsCompleted);
        Assert.IsTrue(tcs2.Task.IsCompleted);
    }

    [TestMethod]
    public async Task UnregisteredAsync_ShouldFireAsyncEvent()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>());

        UnregisteredEventArgs? eventArgs = null;
        var tcs = new TaskCompletionSource<bool>();
        _container.UnregisteredAsync += async (sender, args) =>
        {
            await Task.Yield();
            eventArgs = args;
            tcs.SetResult(true);
        };

        // Act - sync unregister still fires async event
        _container.Unregister<SimpleService>();

        // Assert - wait for async handler to complete (fire-and-forget style)
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
        Assert.IsNotNull(eventArgs);
        Assert.AreEqual(typeof(SimpleService), eventArgs.UnregisteredType);
    }

    [TestMethod]
    public async Task UnregisteredAsync_ShouldFireBothSyncAndAsyncEvents()
    {
        // Arrange
        _container.Configure(c => c.Export<SimpleService>());

        var syncHandlerCalled = false;
        var tcs = new TaskCompletionSource<bool>();

        _container.Unregistered += (sender, args) => syncHandlerCalled = true;
        _container.UnregisteredAsync += async (sender, args) =>
        {
            await Task.Yield();
            tcs.SetResult(true);
        };

        // Act
        _container.Unregister<SimpleService>();

        // Assert
        Assert.IsTrue(syncHandlerCalled);
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
    }

    [TestMethod]
    public async Task UnregisterAsync_DisposableSingleton_ShouldDisposeAsynchronously()
    {
        // Arrange
        _container.Configure(c => c.Export<AsyncDisposableService>().Lifestyle.Singleton());
        var instance = _container.Locate<AsyncDisposableService>();

        // Act - use async unregister for IAsyncDisposable
        await _container.UnregisterAsync<AsyncDisposableService>();

        // Assert
        Assert.IsTrue(instance.IsDisposed);
    }

    [TestMethod]
    public async Task UnregisteredAsync_MultipleTypes_ShouldFireForEach()
    {
        // Arrange
        _container.Configure(c =>
        {
            c.Export<AuthPacketHandler>().As<IPacketHandler>();
            c.Export<GamePacketHandler>().As<IPacketHandler>();
            c.Export<ChatPacketHandler>().As<IPacketHandler>();
        });

        var unregisteredTypes = new List<Type>();
        var tcs = new TaskCompletionSource<bool>();
        var expectedCount = 3;
        _container.UnregisteredAsync += async (sender, args) =>
        {
            await Task.Yield();
            lock (unregisteredTypes)
            {
                unregisteredTypes.Add(args.UnregisteredType);
                if (unregisteredTypes.Count == expectedCount)
                    tcs.TrySetResult(true);
            }
        };

        // Act
        var count = _container.UnregisterAll<IPacketHandler>();

        // Assert
        Assert.AreEqual(3, count);
        await Task.WhenAny(tcs.Task, Task.Delay(1000));
        Assert.IsTrue(tcs.Task.IsCompleted);
        Assert.AreEqual(3, unregisteredTypes.Count);
        CollectionAssert.Contains(unregisteredTypes, typeof(AuthPacketHandler));
        CollectionAssert.Contains(unregisteredTypes, typeof(GamePacketHandler));
        CollectionAssert.Contains(unregisteredTypes, typeof(ChatPacketHandler));
    }
}
