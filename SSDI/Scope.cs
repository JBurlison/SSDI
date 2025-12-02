using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using SSDI.Builder;
using SSDI.Parameters;

namespace SSDI;

/// <summary>
/// A scope for resolving scoped dependencies.
/// </summary>
internal sealed class Scope : IScope
{
    private readonly ActivationBuilder _container;
    private readonly ConcurrentDictionary<Type, object> _scopedInstances = new();
    private readonly ConcurrentBag<object> _disposables = new();
    private volatile bool _disposed;

    internal Scope(ActivationBuilder container)
    {
        _container = container;
    }

    /// <inheritdoc />
    public bool IsDisposed => _disposed;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>() => (T)_container.LocateWithScope(typeof(T), this, []);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Locate(Type type) => _container.LocateWithScope(type, this, []);

    /// <inheritdoc />
    public T LocateWithPositionalParams<T>(params object[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.Positional(i, parameters[i]);
        }
        return (T)_container.LocateWithScope(typeof(T), this, diParams);
    }

    /// <inheritdoc />
    public T LocateWithNamedParameters<T>(params (string name, object value)[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.Named(parameters[i].name, parameters[i].value);
        }
        return (T)_container.LocateWithScope(typeof(T), this, diParams);
    }

    /// <inheritdoc />
    public T LocateWithTypedParams<T>(params object[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.Typed(parameters[i]);
        }
        return (T)_container.LocateWithScope(typeof(T), this, diParams);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>(params DIParameter[] parameters) => (T)_container.LocateWithScope(typeof(T), this, parameters);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>(int position, object value)
    {
        var diParams = new DIParameter[1];
        diParams[0] = DIParameter.Positional(position, value);
        return (T)_container.LocateWithScope(typeof(T), this, diParams);
    }

    /// <summary>
    /// Gets or creates a scoped instance for the specified type.
    /// </summary>
    internal object GetOrAddScoped(Type type, Func<object> factory)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Scope), "Cannot resolve from a disposed scope.");

        return _scopedInstances.GetOrAdd(type, _ =>
        {
            var instance = factory();

            // Track disposables for cleanup
            if (instance is IDisposable or IAsyncDisposable)
            {
                _disposables.Add(instance);
            }

            return instance;
        });
    }

    /// <summary>
    /// Checks if a scoped instance already exists for the specified type.
    /// </summary>
    internal bool TryGetScoped(Type type, out object? instance) => _scopedInstances.TryGetValue(type, out instance);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var disposable in _disposables)
        {
            if (disposable is IDisposable syncDisposable)
            {
                syncDisposable.Dispose();
            }
            else if (disposable is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        _scopedInstances.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var disposable in _disposables)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (disposable is IDisposable syncDisposable)
            {
                syncDisposable.Dispose();
            }
        }

        _scopedInstances.Clear();
    }
}
