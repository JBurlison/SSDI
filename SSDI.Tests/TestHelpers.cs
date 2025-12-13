namespace SSDI.Tests;

// Simple test classes for dependency injection tests

public class SimpleService { }

public class ServiceWithDependency
{
    public SimpleService Dependency { get; }

    public ServiceWithDependency(SimpleService dependency)
    {
        Dependency = dependency;
    }
}

public class ServiceWithMultipleDependencies
{
    public SimpleService Service1 { get; }
    public AnotherService Service2 { get; }

    public ServiceWithMultipleDependencies(SimpleService service1, AnotherService service2)
    {
        Service1 = service1;
        Service2 = service2;
    }
}

public class AnotherService { }

public class ServiceWithParameters
{
    public string Name { get; }
    public int Port { get; }

    public ServiceWithParameters(string name, int port)
    {
        Name = name;
        Port = port;
    }
}

public class ServiceWithMixedParameters
{
    public SimpleService Dependency { get; }
    public string ConnectionString { get; }
    public int Timeout { get; }

    public ServiceWithMixedParameters(SimpleService dependency, string connectionString, int timeout)
    {
        Dependency = dependency;
        ConnectionString = connectionString;
        Timeout = timeout;
    }
}

public class ServiceWithOptionalParameter
{
    public string Name { get; }
    public int Port { get; }

    public ServiceWithOptionalParameter(string name, int port = 8080)
    {
        Name = name;
        Port = port;
    }
}

// Interface implementations for alias tests
public interface IService { }
public interface IPacketHandler { void Handle(); }

public class ServiceImplementation : IService { }
public class AnotherServiceImplementation : IService { }
public class ThirdServiceImplementation : IService { }

public class AuthPacketHandler : IPacketHandler
{
    public void Handle() { }
}

public class GamePacketHandler : IPacketHandler
{
    public void Handle() { }
}

public class ChatPacketHandler : IPacketHandler
{
    public void Handle() { }
}

// Disposable test classes
public class DisposableService : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public class AnotherDisposableService : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public class AsyncDisposableService : IAsyncDisposable
{
    public bool IsDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

// Class with multiple constructors
public class ServiceWithMultipleConstructors
{
    public string? Name { get; }
    public int Port { get; }
    public bool UsedDefaultConstructor { get; }

    public ServiceWithMultipleConstructors()
    {
        UsedDefaultConstructor = true;
    }

    public ServiceWithMultipleConstructors(string name)
    {
        Name = name;
    }

    public ServiceWithMultipleConstructors(string name, int port)
    {
        Name = name;
        Port = port;
    }
}

// Class with three parameters for combined registration/runtime tests
public class ServiceWithThreeParameters
{
    public string Host { get; }
    public int Port { get; }
    public bool UseSsl { get; }

    public ServiceWithThreeParameters(string host, int port, bool useSsl)
    {
        Host = host;
        Port = port;
        UseSsl = useSsl;
    }
}

// Class with four parameters for comprehensive combined tests
public class ServiceWithFourParameters
{
    public string Host { get; }
    public int Port { get; }
    public string Username { get; }
    public string Password { get; }

    public ServiceWithFourParameters(string host, int port, string username, string password)
    {
        Host = host;
        Port = port;
        Username = username;
        Password = password;
    }
}

// Class with mixed DI dependencies and primitive parameters
public class ServiceWithDependencyAndThreeParameters
{
    public SimpleService Dependency { get; }
    public string Host { get; }
    public int Port { get; }
    public bool UseSsl { get; }

    public ServiceWithDependencyAndThreeParameters(SimpleService dependency, string host, int port, bool useSsl)
    {
        Dependency = dependency;
        Host = host;
        Port = port;
        UseSsl = useSsl;
    }
}

// Open generic types for open-generic registration tests
public interface ILogger<T>
{
    Guid Id { get; }
}

public class Logger<T> : ILogger<T>
{
    public Guid Id { get; } = Guid.NewGuid();
}

public class ServiceWithOwnLogger
{
    public ServiceWithMultipleDependencies Dependency { get; }
    public ILogger<ServiceWithOwnLogger> Logger { get; }

    public ServiceWithOwnLogger(ServiceWithMultipleDependencies dependency, ILogger<ServiceWithOwnLogger> logger)
    {
        Dependency = dependency;
        Logger = logger;
    }
}

public class ChildServiceWithOwnLogger
{
    public ILogger<ChildServiceWithOwnLogger> Logger { get; }

    public ChildServiceWithOwnLogger(ILogger<ChildServiceWithOwnLogger> logger)
    {
        Logger = logger;
    }
}

public class ParentServiceWithOwnLogger
{
    public ChildServiceWithOwnLogger Child { get; }
    public ILogger<ParentServiceWithOwnLogger> Logger { get; }

    public ParentServiceWithOwnLogger(ChildServiceWithOwnLogger child, ILogger<ParentServiceWithOwnLogger> logger)
    {
        Child = child;
        Logger = logger;
    }
}
