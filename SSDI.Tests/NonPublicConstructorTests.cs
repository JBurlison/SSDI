namespace SSDI.Tests;

[TestClass]
public class NonPublicConstructorTests
{
    private DependencyInjectionContainer _container = null!;

    [TestInitialize]
    public void Setup()
    {
        _container = new DependencyInjectionContainer();
    }

    [TestMethod]
    public void Locate_WithInternalConstructor_ShouldCreateInstance()
    {
        // Arrange
        _container.Configure(c => c.Export<ServiceWithInternalConstructor>());

        // Act
        var instance = _container.Locate<ServiceWithInternalConstructor>();

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsTrue(instance.WasCreated);
    }

    [TestMethod]
    public void Locate_WithPrivateConstructor_ShouldCreateInstance()
    {
        // Arrange
        _container.Configure(c => c.Export<ServiceWithPrivateConstructor>());

        // Act
        var instance = _container.Locate<ServiceWithPrivateConstructor>();

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsTrue(instance.WasCreated);
    }

    [TestMethod]
    public void Locate_WithProtectedConstructor_ShouldCreateInstance()
    {
        // Arrange - Use concrete derived class that exposes protected constructor behavior
        _container.Configure(c => c.Export<DerivedServiceWithProtectedConstructor>());

        // Act
        var instance = _container.Locate<DerivedServiceWithProtectedConstructor>();

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsTrue(instance.WasCreatedViaProtected);
    }

    [TestMethod]
    public void Locate_WithInternalConstructorAndParameters_ShouldInjectParameters()
    {
        // Arrange
        _container.Configure(c =>
        {
            c.Export<ServiceWithInternalConstructorAndParams>()
                .WithCtorParam("name", "TestService")
                .WithCtorParam("port", 8080);
        });

        // Act
        var instance = _container.Locate<ServiceWithInternalConstructorAndParams>();

        // Assert
        Assert.IsNotNull(instance);
        Assert.AreEqual("TestService", instance.Name);
        Assert.AreEqual(8080, instance.Port);
    }

    [TestMethod]
    public void Locate_WithPrivateConstructorAndDependency_ShouldResolveDependency()
    {
        // Arrange
        _container.Configure(c =>
        {
            c.Export<SimpleService>();
            c.Export<ServiceWithPrivateConstructorAndDependency>();
        });

        // Act
        var instance = _container.Locate<ServiceWithPrivateConstructorAndDependency>();

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsNotNull(instance.Dependency);
    }

    [TestMethod]
    public void Locate_WithMixedConstructorVisibility_ShouldPreferPublicConstructor()
    {
        // Arrange
        _container.Configure(c => c.Export<ServiceWithMixedConstructorVisibility>());

        // Act
        var instance = _container.Locate<ServiceWithMixedConstructorVisibility>();

        // Assert
        Assert.IsNotNull(instance);
        // Public parameterless constructor should be preferred
        Assert.IsTrue(instance.UsedPublicConstructor);
    }

    [TestMethod]
    public void Locate_WithOnlyPrivateConstructorAndParams_ShouldUsePrivateWhenMatched()
    {
        // Arrange
        _container.Configure(c =>
        {
            c.Export<ServiceWithMixedConstructorVisibility>()
                .WithCtorParam("secret", "MySecret");
        });

        // Act
        var instance = _container.Locate<ServiceWithMixedConstructorVisibility>();

        // Assert
        Assert.IsNotNull(instance);
        Assert.AreEqual("MySecret", instance.Secret);
        Assert.IsFalse(instance.UsedPublicConstructor);
    }

    [TestMethod]
    public void Locate_WithInternalConstructor_AsSingleton_ShouldReturnSameInstance()
    {
        // Arrange
        _container.Configure(c =>
            c.Export<ServiceWithInternalConstructor>().Lifestyle.Singleton());

        // Act
        var instance1 = _container.Locate<ServiceWithInternalConstructor>();
        var instance2 = _container.Locate<ServiceWithInternalConstructor>();

        // Assert
        Assert.AreSame(instance1, instance2);
    }

    [TestMethod]
    public void Locate_WithInternalConstructor_AsTransient_ShouldReturnDifferentInstances()
    {
        // Arrange
        _container.Configure(c =>
            c.Export<ServiceWithInternalConstructor>().Lifestyle.Transient());

        // Act
        var instance1 = _container.Locate<ServiceWithInternalConstructor>();
        var instance2 = _container.Locate<ServiceWithInternalConstructor>();

        // Assert
        Assert.AreNotSame(instance1, instance2);
    }

    [TestMethod]
    public void Locate_WithInternalConstructor_AsInterface_ShouldWork()
    {
        // Arrange
        _container.Configure(c =>
            c.Export<InternalConstructorServiceImpl>().As<IService>());

        // Act
        var instance = _container.Locate<IService>();

        // Assert
        Assert.IsNotNull(instance);
        Assert.IsInstanceOfType(instance, typeof(InternalConstructorServiceImpl));
    }

    [TestMethod]
    public void Locate_MultipleNonPublicImplementations_AsEnumerable_ShouldResolveAll()
    {
        // Arrange
        _container.Configure(c =>
        {
            c.Export<InternalConstructorServiceImpl>().As<IService>();
            c.Export<PrivateConstructorServiceImpl>().As<IService>();
        });

        // Act
        var instances = _container.Locate<IEnumerable<IService>>().ToList();

        // Assert
        Assert.AreEqual(2, instances.Count());
    }

    [TestMethod]
    public void Unregister_WithNonPublicConstructor_ShouldWork()
    {
        // Arrange
        _container.Configure(c =>
            c.Export<ServiceWithInternalConstructor>());

        // Act
        var removed = _container.Unregister<ServiceWithInternalConstructor>();

        // Assert
        Assert.IsTrue(removed);
        Assert.IsFalse(_container.IsRegistered<ServiceWithInternalConstructor>());
    }

    [TestMethod]
    public void Locate_WithInternalConstructor_InScope_ShouldWork()
    {
        // Arrange
        _container.Configure(c =>
            c.Export<ServiceWithInternalConstructor>().Lifestyle.Scoped());

        // Act
        using var scope = _container.CreateScope();
        var instance1 = scope.Locate<ServiceWithInternalConstructor>();
        var instance2 = scope.Locate<ServiceWithInternalConstructor>();

        // Assert
        Assert.IsNotNull(instance1);
        Assert.AreSame(instance1, instance2);
    }
}

// Test helper classes for non-public constructor tests

public class ServiceWithInternalConstructor
{
    public bool WasCreated { get; }

    internal ServiceWithInternalConstructor()
    {
        WasCreated = true;
    }
}

public class ServiceWithPrivateConstructor
{
    public bool WasCreated { get; }

    private ServiceWithPrivateConstructor()
    {
        WasCreated = true;
    }
}

public class BaseServiceWithProtectedConstructor
{
    public bool WasCreatedViaProtected { get; protected set; }

    protected BaseServiceWithProtectedConstructor()
    {
        WasCreatedViaProtected = true;
    }
}

public class DerivedServiceWithProtectedConstructor : BaseServiceWithProtectedConstructor
{
    // This will use the protected base constructor
}

public class ServiceWithInternalConstructorAndParams
{
    public string Name { get; }
    public int Port { get; }

    internal ServiceWithInternalConstructorAndParams(string name, int port)
    {
        Name = name;
        Port = port;
    }
}

public class ServiceWithPrivateConstructorAndDependency
{
    public SimpleService Dependency { get; }

    private ServiceWithPrivateConstructorAndDependency(SimpleService dependency)
    {
        Dependency = dependency;
    }
}

public class ServiceWithMixedConstructorVisibility
{
    public bool UsedPublicConstructor { get; }
    public string? Secret { get; }

    public ServiceWithMixedConstructorVisibility()
    {
        UsedPublicConstructor = true;
    }

    private ServiceWithMixedConstructorVisibility(string secret)
    {
        Secret = secret;
        UsedPublicConstructor = false;
    }
}

public class InternalConstructorServiceImpl : IService
{
    internal InternalConstructorServiceImpl() { }
}

public class PrivateConstructorServiceImpl : IService
{
    private PrivateConstructorServiceImpl() { }
}
