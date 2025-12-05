using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using SSDI.Events;
using SSDI.Parameters;
using SSDI.Registration;

namespace SSDI.Builder;

/// <summary>
/// Provides the core activation and resolution logic for the dependency injection container.
/// This class is thread-safe with a lock-free read path for maximum performance:
/// - Resolution (Locate) operations are lock-free, using ConcurrentDictionary for thread-safety
/// - Registration and unregistration operations use write locks for consistency
/// All operations can be called concurrently from multiple threads without external synchronization.
/// </summary>
public class ActivationBuilder
{
    #region Fields

    // Static fields
    private static readonly List<DIParameter> EmptyParameters = new(0);
    private static readonly ConcurrentDictionary<int, ActivationBuilder> _containers = new();
    private static int _nextContainerId;

    // Instance fields - internal
    internal readonly int ContainerId = Interlocked.Increment(ref _nextContainerId);

    // Thread-safety: ReaderWriterLockSlim for optimal read-heavy workloads
    // Reads (resolution) can run concurrently, writes (registration/unregistration) are exclusive
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

    /// <summary>
    /// When true, factories are compiled at registration time for optimal resolution performance.
    /// When false (default), factories are compiled lazily on first resolution for faster registration.
    /// </summary>
    public bool EagerCompilation { get; set; } = false;

    // Instance fields - private (registration & caching)
    private readonly ConcurrentDictionary<Type, TypeRegistration> _registrations = new();
    private readonly ConcurrentDictionary<Type, List<Type>> _aliases = new();
    private readonly ConcurrentDictionary<Type, Func<Scope?, object>> _factoryCache = new();
    private readonly ConcurrentDictionary<Type, Delegate> _genericFactoryCache = new();
    private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();
    private readonly ConcurrentDictionary<Type, Func<Scope?, object>> _enumerableFactoryCache = new();

    // Dependency tracking for cascade invalidation (reverse dependency map)
    // Key = dependency type, Value = set of types that depend on it
    private readonly ConcurrentDictionary<Type, HashSet<Type>> _dependents = new();

    // Forward dependency map for cleanup
    // Key = type, Value = set of types it depends on
    private readonly ConcurrentDictionary<Type, HashSet<Type>> _dependencies = new();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivationBuilder"/> class.
    /// </summary>
    public ActivationBuilder()
    {
        _containers[ContainerId] = this;
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when a type is registered with the container.
    /// </summary>
    public event EventHandler<RegisteredEventArgs>? Registered;

    /// <summary>
    /// Occurs asynchronously when a type is registered with the container.
    /// All async handlers are awaited before registration completes.
    /// </summary>
    public event Func<object?, RegisteredEventArgs, Task>? RegisteredAsync;

    /// <summary>
    /// Occurs when a type is unregistered from the container.
    /// </summary>
    public event EventHandler<UnregisteredEventArgs>? Unregistered;

    /// <summary>
    /// Occurs asynchronously when a type is unregistered from the container.
    /// All async handlers are awaited before unregistration completes.
    /// </summary>
    public event Func<object?, UnregisteredEventArgs, Task>? UnregisteredAsync;

    #endregion

    #region Public Methods - Registration

    /// <summary>
    /// Unregisters a type from the container.
    /// </summary>
    /// <typeparam name="T">The type to unregister.</typeparam>
    /// <param name="removeFromAliases">If true, also removes this type from any alias registrations.</param>
    /// <returns>True if the type was found and removed; otherwise, false.</returns>
    public bool Unregister<T>(bool removeFromAliases = true) => Unregister(typeof(T), removeFromAliases);

    /// <summary>
    /// Unregisters a type from the container.
    /// </summary>
    /// <param name="type">The type to unregister.</param>
    /// <param name="removeFromAliases">If true, also removes this type from any alias registrations.</param>
    /// <returns>True if the type was found and removed; otherwise, false.</returns>
    public bool Unregister(Type type, bool removeFromAliases = true)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = false;
            object? removedInstance = null;
            var wasDisposed = false;

            if (_registrations.TryRemove(type, out _)) removed = true;

            if (_singletonInstances.TryRemove(type, out var instance))
            {
                removedInstance = instance;
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                    wasDisposed = true;
                }
#if NET8_0_OR_GREATER
                else if (instance is IAsyncDisposable asyncDisposable)
                {
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    wasDisposed = true;
                }
#endif
                removed = true;
            }

            _factoryCache.TryRemove(type, out _);
            _genericFactoryCache.TryRemove(type, out _);

            // Cascade invalidate all dependents (full transitive closure)
            InvalidateDependentsRecursive(type);

            // Clean up dependency tracking for this type
            CleanupDependencyTracking(type);

            if (removeFromAliases)
            {
                foreach (var kvp in _aliases)
                {
                    if (kvp.Value.Remove(type))
                    {
                        _factoryCache.TryRemove(kvp.Key, out _);
                        _genericFactoryCache.TryRemove(kvp.Key, out _);
                        _enumerableFactoryCache.TryRemove(typeof(IEnumerable<>).MakeGenericType(kvp.Key), out _);

                        // Also invalidate dependents of the alias
                        InvalidateDependentsRecursive(kvp.Key);
                    }
                }
            }

            if (removed)
            {
                var eventArgs = new UnregisteredEventArgs(type, removedInstance, wasDisposed);
                Unregistered?.Invoke(this, eventArgs);
                FireUnregisteredAsync(eventArgs);
            }

            return removed;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Asynchronously unregisters a type from the container.
    /// </summary>
    /// <typeparam name="T">The type to unregister.</typeparam>
    /// <param name="removeFromAliases">If true, also removes this type from any alias registrations.</param>
    /// <returns>A task that represents the async operation, containing true if the type was found and removed.</returns>
    public Task<bool> UnregisterAsync<T>(bool removeFromAliases = true) => UnregisterAsync(typeof(T), removeFromAliases);

    /// <summary>
    /// Asynchronously unregisters a type from the container.
    /// </summary>
    /// <param name="type">The type to unregister.</param>
    /// <param name="removeFromAliases">If true, also removes this type from any alias registrations.</param>
    /// <returns>A task that represents the async operation, containing true if the type was found and removed.</returns>
    public async Task<bool> UnregisterAsync(Type type, bool removeFromAliases = true)
    {
        _lock.EnterWriteLock();
        try
        {
            var removed = false;
            object? removedInstance = null;
            var wasDisposed = false;

            if (_registrations.TryRemove(type, out _)) removed = true;

            if (_singletonInstances.TryRemove(type, out var instance))
            {
                removedInstance = instance;
#if NET8_0_OR_GREATER
                if (instance is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    wasDisposed = true;
                }
                else
#endif
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                    wasDisposed = true;
                }
                removed = true;
            }

            _factoryCache.TryRemove(type, out _);
            _genericFactoryCache.TryRemove(type, out _);

            // Cascade invalidate all dependents (full transitive closure)
            InvalidateDependentsRecursive(type);

            // Clean up dependency tracking for this type
            CleanupDependencyTracking(type);

            if (removeFromAliases)
            {
                foreach (var kvp in _aliases)
                {
                    if (kvp.Value.Remove(type))
                    {
                        _factoryCache.TryRemove(kvp.Key, out _);
                        _genericFactoryCache.TryRemove(kvp.Key, out _);
                        _enumerableFactoryCache.TryRemove(typeof(IEnumerable<>).MakeGenericType(kvp.Key), out _);

                        // Also invalidate dependents of the alias
                        InvalidateDependentsRecursive(kvp.Key);
                    }
                }
            }

            if (removed)
            {
                var eventArgs = new UnregisteredEventArgs(type, removedInstance, wasDisposed);
                Unregistered?.Invoke(this, eventArgs);
                FireUnregisteredAsync(eventArgs);
            }

            return removed;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Asynchronously unregisters all implementations registered under an alias type.
    /// </summary>
    /// <typeparam name="TAlias">The alias type to unregister all implementations for.</typeparam>
    /// <returns>A task that represents the async operation, containing the number of implementations unregistered.</returns>
    public Task<int> UnregisterAllAsync<TAlias>() => UnregisterAllAsync(typeof(TAlias));

    /// <summary>
    /// Asynchronously unregisters all implementations registered under an alias type.
    /// </summary>
    /// <param name="aliasType">The alias type to unregister all implementations for.</param>
    /// <returns>A task that represents the async operation, containing the number of implementations unregistered.</returns>
    public async Task<int> UnregisterAllAsync(Type aliasType)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_aliases.TryRemove(aliasType, out var implementations)) return 0;

            var count = 0;
            var implCopy = new List<Type>(implementations);

            // Release write lock before calling UnregisterAsync to avoid deadlock
            // since UnregisterAsync also acquires the write lock
            _lock.ExitWriteLock();
            try
            {
                foreach (var implType in implCopy)
                {
                    if (await UnregisterAsync(implType, removeFromAliases: false).ConfigureAwait(false)) count++;
                }
            }
            finally
            {
                _lock.EnterWriteLock();
            }

            _factoryCache.TryRemove(aliasType, out _);
            _genericFactoryCache.TryRemove(aliasType, out _);
            _enumerableFactoryCache.TryRemove(typeof(IEnumerable<>).MakeGenericType(aliasType), out _);

            // Invalidate dependents of the alias
            InvalidateDependentsRecursive(aliasType);

            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Unregisters all implementations registered under an alias type.
    /// </summary>
    /// <typeparam name="TAlias">The alias type to unregister all implementations for.</typeparam>
    /// <returns>The number of implementations that were unregistered.</returns>
    public int UnregisterAll<TAlias>() => UnregisterAll(typeof(TAlias));

    /// <summary>
    /// Unregisters all implementations registered under an alias type.
    /// </summary>
    /// <param name="aliasType">The alias type to unregister all implementations for.</param>
    /// <returns>The number of implementations that were unregistered.</returns>
    public int UnregisterAll(Type aliasType)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_aliases.TryRemove(aliasType, out var implementations)) return 0;

            var count = 0;
            var implCopy = new List<Type>(implementations);

            // Release write lock before calling Unregister to avoid deadlock
            // since Unregister also acquires the write lock
            _lock.ExitWriteLock();
            try
            {
                foreach (var implType in implCopy)
                {
                    if (Unregister(implType, removeFromAliases: false)) count++;
                }
            }
            finally
            {
                _lock.EnterWriteLock();
            }

            _factoryCache.TryRemove(aliasType, out _);
            _genericFactoryCache.TryRemove(aliasType, out _);
            _enumerableFactoryCache.TryRemove(typeof(IEnumerable<>).MakeGenericType(aliasType), out _);

            // Invalidate dependents of the alias
            InvalidateDependentsRecursive(aliasType);

            return count;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Checks if a type is registered in the container.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>True if the type is registered; otherwise, false.</returns>
    public bool IsRegistered<T>() => IsRegistered(typeof(T));

    /// <summary>
    /// Checks if a type is registered in the container.
    /// Lock-free - ConcurrentDictionary provides thread-safety for reads.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is registered; otherwise, false.</returns>
    public bool IsRegistered(Type type)
    {
        return _registrations.ContainsKey(type) ||
            _aliases.ContainsKey(type) ||
            _singletonInstances.ContainsKey(type);
    }

    #endregion

    #region Public Methods - Generic Resolution

    /// <summary>
    /// Ultra-fast generic locate - uses cached Func&lt;T&gt; to avoid boxing.
    /// Lock-free for maximum performance - ConcurrentDictionary provides thread-safety.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>()
    {
        if (_genericFactoryCache.TryGetValue(typeof(T), out var cached))
        {
            return ((Func<T>)cached)();
        }

        return LocateAndCacheGeneric<T>();
    }

    /// <summary>
    /// Locates and returns an instance with custom DI parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Custom <see cref="IDIParameter"/> instances.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public T Locate<T>(params IDIParameter[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            diParams[i] = DIParameter.FromLegacy(parameters[i]);
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with a single positional parameter.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="position">The zero-based position of the parameter.</param>
    /// <param name="value">The value for the parameter.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>(int position, object value)
    {
        return (T)Locate(typeof(T), new[] { DIParameter.Positional(position, value) });
    }

    /// <summary>
    /// Locates and returns an instance with struct-based parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Struct-based parameters for optimal performance.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>(params DIParameter[] parameters)
    {
        if (parameters.Length == 0) return Locate<T>();
        return (T)Locate(typeof(T), parameters);
    }

    /// <summary>
    /// Locates and returns an instance with positional constructor parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">The positional parameter values.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public T LocateWithPositionalParams<T>(params object[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            diParams[i] = DIParameter.Positional(i, parameters[i]);
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with named constructor parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Tuples of parameter names and values.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public T LocateWithNamedParameters<T>(params (string name, object value)[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            diParams[i] = DIParameter.Named(parameters[i].name, parameters[i].value);
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with typed constructor parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">The parameter values to match by type.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public T LocateWithTypedParams<T>(params object[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            diParams[i] = DIParameter.Typed(parameters[i]);
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with custom DI parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Custom <see cref="IDIParameter"/> instances.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    public T LocateWithParams<T>(params IDIParameter[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            diParams[i] = DIParameter.FromLegacy(parameters[i]);
        return (T)Locate(typeof(T), diParams);
    }

    #endregion

    #region Public Methods - Non-Generic Resolution

    /// <summary>
    /// Locates and returns an instance of the specified type.
    /// Lock-free for maximum performance - ConcurrentDictionary provides thread-safety.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>An instance of the specified type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Locate(Type type)
    {
        return LocateWithScope(type, null, Array.Empty<DIParameter>());
    }

    /// <summary>
    /// Locates and returns an instance of the specified type with custom DI parameters.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <param name="parameters">Custom <see cref="IDIParameter"/> instances.</param>
    /// <returns>An instance of the specified type.</returns>
    public object Locate(Type type, params IDIParameter[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            diParams[i] = DIParameter.FromLegacy(parameters[i]);
        return Locate(type, diParams);
    }

    /// <summary>
    /// Locates and returns an instance of the specified type with struct-based parameters.
    /// Lock-free for maximum performance - ConcurrentDictionary provides thread-safety.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <param name="parameters">Struct-based parameters.</param>
    /// <returns>An instance of the specified type.</returns>
    public object Locate(Type type, params DIParameter[] parameters)
    {
        return LocateWithScope(type, null, parameters);
    }

    #endregion

    #region Public Methods - Scope Management

    /// <summary>
    /// Creates a new scope for resolving scoped services.
    /// </summary>
    /// <returns>A new <see cref="IScope"/> instance.</returns>
    public IScope CreateScope() => new Scope(this);

    #endregion

    #region Internal Methods

    internal void Add(ExportRegistration reg)
    {
        _lock.EnterWriteLock();
        try
        {
            AddInternal(reg);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void AddInternal(ExportRegistration reg)
    {
        // First pass: register all types (metadata only)
        var typesToCompile = new List<Type>();
        var registeredTypes = new List<(Type type, IReadOnlyList<Type> aliases, LifestyleType lifestyle, bool hasInstance)>();

        foreach (var exportRegistration in reg.Registrations)
        {
            var exportedType = exportRegistration.ExportedType;
            var fluentReg = exportRegistration.FluentExportRegistration;
            var lifestyle = fluentReg.LifestyleValue;
            var aliases = fluentReg.HasAlias ? fluentReg.Alias.ToList() : (IReadOnlyList<Type>)Array.Empty<Type>();

            var parameters = fluentReg.HasParameters ? fluentReg.ParametersInternal : EmptyParameters;

            if (lifestyle == LifestyleType.Singleton && exportRegistration.Instance is not null)
            {
                var instance = exportRegistration.Instance;
                _singletonInstances[exportedType] = instance;
                _factoryCache[exportedType] = _ => instance;

                if (fluentReg.HasAlias)
                {
                    foreach (var aliasType in fluentReg.Alias)
                    {
                        AddAliasFast(aliasType, exportedType);
                        _factoryCache[aliasType] = _ => instance;
                    }
                }

                _registrations[exportedType] = new TypeRegistration(exportedType, lifestyle, parameters, instance);
                registeredTypes.Add((exportedType, aliases, lifestyle, true));
                continue;
            }

            _registrations[exportedType] = new TypeRegistration(exportedType, lifestyle, parameters, null);
            typesToCompile.Add(exportedType);
            registeredTypes.Add((exportedType, aliases, lifestyle, false));

            if (fluentReg.HasAlias)
            {
                foreach (var aliasType in fluentReg.Alias)
                {
                    AddAliasFast(aliasType, exportedType);
                    // Invalidate any existing cache for this alias
                    _factoryCache.TryRemove(aliasType, out _);
                    _genericFactoryCache.TryRemove(aliasType, out _);
                }
            }

            // Invalidate existing cache for this type
            _factoryCache.TryRemove(exportedType, out _);
            _genericFactoryCache.TryRemove(exportedType, out _);
        }

        // Second pass: eagerly compile factories for all registered types (if enabled)
        if (EagerCompilation)
        {
            foreach (var type in typesToCompile)
            {
                EagerCompileFactory(type);
            }
        }

        // Fire registration events for all registered types
        foreach (var (type, aliases, lifestyle, hasInstance) in registeredTypes)
        {
            var eventArgs = new RegisteredEventArgs(type, aliases, lifestyle, hasInstance);
            Registered?.Invoke(this, eventArgs);
            FireRegisteredAsync(eventArgs);
        }
    }

    /// <summary>
    /// Lock-free resolution with scope support.
    /// </summary>
    internal object LocateWithScope(Type type, Scope? scope, DIParameter[] parameters)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return ResolveEnumerable(type, scope, parameters);
        }

        if (parameters.Length == 0)
        {
            if (_factoryCache.TryGetValue(type, out var factory))
            {
                return factory(scope);
            }
            return LocateInternalByType(type, scope);
        }

        return LocateWithRuntimeParams(type, scope, parameters);
    }

    #endregion

    #region Private Methods - Event Helpers

    /// <summary>
    /// Invokes all async handlers for the RegisteredAsync event (fire-and-forget).
    /// </summary>
    private void FireRegisteredAsync(RegisteredEventArgs eventArgs)
    {
        var handlers = RegisteredAsync;
        if (handlers == null) return;

        foreach (var handler in handlers.GetInvocationList())
        {
            _ = ((Func<object?, RegisteredEventArgs, Task>)handler)(this, eventArgs);
        }
    }

    /// <summary>
    /// Invokes all async handlers for the UnregisteredAsync event (fire-and-forget).
    /// </summary>
    private void FireUnregisteredAsync(UnregisteredEventArgs eventArgs)
    {
        var handlers = UnregisteredAsync;
        if (handlers == null) return;

        foreach (var handler in handlers.GetInvocationList())
        {
            _ = ((Func<object?, UnregisteredEventArgs, Task>)handler)(this, eventArgs);
        }
    }

    #endregion

    #region Private Methods - Dependency Tracking

    /// <summary>
    /// Records that 'dependent' depends on 'dependency'
    /// </summary>
    private void TrackDependency(Type dependent, Type dependency)
    {
        // Add to reverse map (dependency -> dependents)
        var dependents = _dependents.GetOrAdd(dependency, _ => new HashSet<Type>());
        lock (dependents)
        {
            dependents.Add(dependent);
        }

        // Add to forward map (dependent -> dependencies)
        var dependencies = _dependencies.GetOrAdd(dependent, _ => new HashSet<Type>());
        lock (dependencies)
        {
            dependencies.Add(dependency);
        }
    }

    /// <summary>
    /// Invalidates all types that depend on the given type (full transitive closure)
    /// </summary>
    private void InvalidateDependentsRecursive(Type type)
    {
        var visited = new HashSet<Type>();
        var toInvalidate = new Queue<Type>();
        toInvalidate.Enqueue(type);

        while (toInvalidate.Count > 0)
        {
            var current = toInvalidate.Dequeue();
            if (!visited.Add(current)) continue;

            // Invalidate caches for this type
            _factoryCache.TryRemove(current, out _);
            _genericFactoryCache.TryRemove(current, out _);

            // Find all types that depend on this type
            if (_dependents.TryGetValue(current, out var dependents))
            {
                List<Type> dependentsCopy;
                lock (dependents)
                {
                    dependentsCopy = new List<Type>(dependents);
                }

                foreach (var dependent in dependentsCopy)
                {
                    if (!visited.Contains(dependent))
                    {
                        toInvalidate.Enqueue(dependent);
                    }
                }
            }

            // Also check aliases - if current is an alias, invalidate the concrete type's dependents
            if (_aliases.TryGetValue(current, out var implementations))
            {
                foreach (var impl in implementations)
                {
                    if (!visited.Contains(impl))
                    {
                        toInvalidate.Enqueue(impl);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Cleans up dependency tracking entries for a type being unregistered
    /// </summary>
    private void CleanupDependencyTracking(Type type)
    {
        // Remove from forward map and clean up reverse entries
        if (_dependencies.TryRemove(type, out var dependencies))
        {
            lock (dependencies)
            {
                foreach (var dep in dependencies)
                {
                    if (_dependents.TryGetValue(dep, out var depSet))
                    {
                        lock (depSet)
                        {
                            depSet.Remove(type);
                        }
                    }
                }
            }
        }

        // Remove the reverse map entry for this type
        _dependents.TryRemove(type, out _);
    }

    // Cached reflection for BuildGenericFactory<T>
    private static readonly MethodInfo _buildGenericFactoryMethod =
        typeof(ActivationBuilder).GetMethod(nameof(BuildGenericFactory), BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Eagerly compiles the factory for a type and tracks its dependencies.
    /// If compilation fails (e.g., due to unresolvable runtime parameters),
    /// silently skips and falls back to lazy compilation at resolve time.
    /// </summary>
    private void EagerCompileFactory(Type type)
    {
        if (_factoryCache.ContainsKey(type)) return;

        // Clear any existing dependency tracking for this type (in case of re-registration)
        if (_dependencies.TryGetValue(type, out var oldDeps))
        {
            lock (oldDeps)
            {
                foreach (var dep in oldDeps)
                {
                    if (_dependents.TryGetValue(dep, out var depSet))
                    {
                        lock (depSet)
                        {
                            depSet.Remove(type);
                        }
                    }
                }
                oldDeps.Clear();
            }
        }

        try
        {
            // Compile with dependency tracking for non-generic path
            var factory = CompileFactoryWithTracking(type, type);
            _factoryCache.TryAdd(type, factory);

            // Also compile and cache the generic factory for this concrete type
            EagerCompileGenericFactory(type);

            // Also compile for any aliases pointing to this type
            foreach (var kvp in _aliases)
            {
                if (kvp.Value.Contains(type) && !_factoryCache.ContainsKey(kvp.Key))
                {
                    try
                    {
                        // The alias points to this type - create a factory for the alias
                        var aliasFactory = CompileFactoryWithTracking(type, kvp.Key);
                        _factoryCache.TryAdd(kvp.Key, aliasFactory);

                        // Also compile and cache the generic factory for this alias
                        EagerCompileGenericFactory(kvp.Key);
                    }
                    catch (InvalidOperationException)
                    {
                        // Skip this alias - will fall back to lazy compilation
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Type cannot be eagerly compiled (e.g., requires runtime parameters)
            // Will fall back to lazy compilation at resolve time
        }
    }

    /// <summary>
    /// Compiles and caches the generic factory for a type using reflection
    /// </summary>
    private void EagerCompileGenericFactory(Type type)
    {
        if (_genericFactoryCache.ContainsKey(type)) return;

        try
        {
            var genericMethod = _buildGenericFactoryMethod.MakeGenericMethod(type);
            var genericFactory = genericMethod.Invoke(this, null) as Delegate;
            if (genericFactory != null)
            {
                _genericFactoryCache.TryAdd(type, genericFactory);
            }
        }
        catch
        {
            // Ignore failures - will fall back to lazy compilation
        }
    }

    /// <summary>
    /// Compiles a factory and tracks dependencies
    /// </summary>
    private Func<Scope?, object> CompileFactoryWithTracking(Type concreteType, Type requestedType)
    {
        if (_singletonInstances.TryGetValue(concreteType, out var existingInstance))
        {
            return _ => existingInstance;
        }

        if (!_registrations.TryGetValue(concreteType, out var registration))
        {
            return CompileFallbackFactory(concreteType);
        }

        if (registration.Instance != null)
        {
            var instance = registration.Instance;
            return _ => instance;
        }

        var lifestyle = registration.Lifestyle;
        var (factoryExpr, dependencies) = BuildFactoryExpressionWithTracking(concreteType, registration);

        // Record dependencies
        foreach (var dep in dependencies)
        {
            TrackDependency(requestedType, dep);
        }

        var compiledFactory = factoryExpr.Compile();

        return lifestyle switch
        {
            LifestyleType.Singleton => CreateSingletonFactoryNonGeneric(concreteType, compiledFactory),
            LifestyleType.Scoped => CreateScopedFactory(concreteType, compiledFactory),
            _ => compiledFactory
        };
    }

    #endregion

    #region Private Methods - Generic Resolution

    [MethodImpl(MethodImplOptions.NoInlining)]
    private T LocateAndCacheGeneric<T>()
    {
        var factory = BuildGenericFactory<T>();
        _genericFactoryCache.TryAdd(typeof(T), factory);
        return factory();
    }

    private Func<T> BuildGenericFactory<T>()
    {
        var type = typeof(T);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return BuildEnumerableFactory<T>(type);
        }

        Type? concreteType = null;
        if (_aliases.TryGetValue(type, out var implementations))
        {
            if (implementations.Count > 0)
                concreteType = implementations[0];
        }

        var resolveType = concreteType ?? type;

        if (_singletonInstances.TryGetValue(resolveType, out var singleton))
        {
            var s = (T)singleton;
            return () => s;
        }

        if (!_registrations.TryGetValue(resolveType, out var registration))
        {
            return BuildFallbackFactory<T>(resolveType);
        }

        if (registration.Instance != null)
        {
            var inst = (T)registration.Instance;
            return () => inst;
        }

        var lifestyle = registration.Lifestyle;
        var (factoryExpr, dependencies) = BuildTypedFactoryExpressionWithTracking<T>(resolveType, registration);

        // Record dependencies
        foreach (var dep in dependencies)
        {
            TrackDependency(type, dep);
        }

        var compiled = factoryExpr.Compile();

        return lifestyle switch
        {
            LifestyleType.Singleton => CreateSingletonFactory<T>(resolveType, compiled),
            LifestyleType.Scoped => throw new InvalidOperationException(
                $"Cannot resolve scoped service '{type.FullName}' from the root container. " +
                "Use container.CreateScope() and resolve from the scope instead."),
            _ => compiled
        };
    }

    private Func<T> CreateSingletonFactory<T>(Type type, Func<T> innerFactory)
    {
        // Use a closure to cache the instance locally after first access.
        // This gives us the fast path (local variable check) while still
        // supporting the shared singleton cache for cross-alias resolution.
        T? localInstance = default;
        var hasLocal = false;

        return () =>
        {
            // Fastest path: local cache hit
            if (hasLocal) return localInstance!;

            // Check if another factory (for an alias) already created this singleton
            if (_singletonInstances.TryGetValue(type, out var existing))
            {
                localInstance = (T)existing;
                hasLocal = true;
                return localInstance;
            }

            // Create singleton and cache it both locally and in the shared cache
            localInstance = (T)_singletonInstances.GetOrAdd(type, _ => innerFactory()!);
            hasLocal = true;
            return localInstance;
        };
    }

    private Func<T> BuildFallbackFactory<T>(Type type)
    {
        var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

        if (ctor != null)
        {
            var newExpr = Expression.New(ctor);
            Expression body = typeof(T) == type
                ? newExpr
                : Expression.Convert(newExpr, typeof(T));
            var lambda = Expression.Lambda<Func<T>>(body);
            return lambda.Compile();
        }

        return () => (T)Activator.CreateInstance(type)!;
    }

    private Func<T> BuildEnumerableFactory<T>(Type enumerableType)
    {
        var elementType = enumerableType.GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(elementType);

        if (_aliases.TryGetValue(elementType, out var implementations))
        {
            if (implementations.Count == 0)
            {
                return () => (T)Activator.CreateInstance(listType)!;
            }

            var factories = implementations.Select(t => GetOrCreateFactory(t)).ToList();
            return () =>
            {
                var list = (IList)Activator.CreateInstance(listType)!;
                foreach (var factory in factories)
                {
                    list.Add(factory(null));
                }
                return (T)list;
            };
        }

        if (_registrations.ContainsKey(elementType))
        {
            var factory = GetOrCreateFactory(elementType);
            return () =>
            {
                var list = (IList)Activator.CreateInstance(listType)!;
                list.Add(factory(null));
                return (T)list;
            };
        }

        return () => (T)Activator.CreateInstance(listType)!;
    }

    #endregion

    #region Private Methods - Non-Generic Resolution

    private object LocateInternalByType(Type type, Scope? scope)
    {
        if (_aliases.TryGetValue(type, out var implementations))
        {
            Type? concreteType = null;
            if (implementations.Count > 0)
                concreteType = implementations[0];

            if (concreteType != null)
            {
                var factory = GetOrCreateFactory(concreteType);
                _factoryCache.TryAdd(type, factory);
                return factory(scope);
            }
        }

        var directFactory = GetOrCreateFactory(type);
        return directFactory(scope);
    }

    private Func<Scope?, object> GetOrCreateFactory(Type type)
    {
        return _factoryCache.GetOrAdd(type, t => CompileFactoryWithTracking(t, t));
    }

    private Func<Scope?, object> CreateSingletonFactoryNonGeneric(Type type, Func<Scope?, object> innerFactory)
    {
        object? instance = null;
        var syncRoot = new object();

        return scope =>
        {
            if (instance != null) return instance;

            lock (syncRoot)
            {
                if (instance != null) return instance;
                instance = innerFactory(scope);
                _singletonInstances.TryAdd(type, instance);
                return instance;
            }
        };
    }

    private Func<Scope?, object> CreateScopedFactory(Type type, Func<Scope?, object> innerFactory)
    {
        return scope =>
        {
            if (scope == null)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve scoped service '{type.FullName}' from the root container. " +
                    "Use container.CreateScope() and resolve from the scope instead.");
            }

            return scope.GetOrAddScoped(type, () => innerFactory(scope));
        };
    }

    private Func<Scope?, object> CompileFallbackFactory(Type type)
    {
        var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

        if (ctor != null)
        {
            var newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda<Func<object>>(Expression.Convert(newExpr, typeof(object)));
            var compiled = lambda.Compile();
            return _ => compiled();
        }

        return _ => Activator.CreateInstance(type)!;
    }

    private object LocateWithRuntimeParams(Type type, Scope? scope, DIParameter[] parameters)
    {
        Type resolveType = type;

        if (_aliases.TryGetValue(type, out var implementations))
        {
            if (implementations.Count > 0)
                resolveType = implementations[0];
        }

        if (!_registrations.TryGetValue(resolveType, out var registration))
        {
            return Activator.CreateInstance(resolveType)!;
        }

        var lifestyle = registration.Lifestyle;

        if (lifestyle == LifestyleType.Scoped && scope == null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve scoped service '{type.FullName}' from the root container. " +
                "Use container.CreateScope() and resolve from the scope instead.");
        }

        if (lifestyle == LifestyleType.Singleton && _singletonInstances.TryGetValue(resolveType, out var singleton))
        {
            return singleton;
        }

        if (lifestyle == LifestyleType.Scoped && scope != null && scope.TryGetScoped(resolveType, out var scoped) && scoped != null)
        {
            return scoped;
        }

        var constructors = resolveType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var ctor in constructors)
        {
            if (TryCreateInstance(ctor, registration, parameters, scope, out var instance))
            {
                if (lifestyle == LifestyleType.Singleton)
                {
                    _singletonInstances.TryAdd(resolveType, instance);
                }
                else if (lifestyle == LifestyleType.Scoped && scope != null)
                {
                    scope.GetOrAddScoped(resolveType, () => instance);
                }

                return instance;
            }
        }

        throw new InvalidOperationException($"Could not create instance of type {resolveType.FullName}");
    }

    #endregion

    #region Private Methods - Enumerable Resolution

    private object ResolveEnumerable(Type enumerableType, Scope? scope, DIParameter[] parameters)
    {
        var elementType = enumerableType.GetGenericArguments()[0];

        if (parameters.Length == 0 && _enumerableFactoryCache.TryGetValue(enumerableType, out var cachedFactory))
        {
            return cachedFactory(scope);
        }

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;

        if (_aliases.TryGetValue(elementType, out var implementations))
        {
            foreach (var implType in implementations)
            {
                if (parameters.Length > 0)
                {
                    list.Add(LocateWithRuntimeParams(implType, scope, parameters));
                }
                else
                {
                    if (_factoryCache.TryGetValue(implType, out var factory))
                    {
                        list.Add(factory(scope));
                    }
                    else
                    {
                        list.Add(LocateInternalByType(implType, scope));
                    }
                }
            }
        }
        else if (_registrations.ContainsKey(elementType))
        {
            if (parameters.Length > 0)
            {
                list.Add(LocateWithRuntimeParams(elementType, scope, parameters));
            }
            else
            {
                list.Add(LocateInternalByType(elementType, scope));
            }
        }

        if (parameters.Length == 0 && list.Count > 0)
        {
            _enumerableFactoryCache.TryAdd(enumerableType, CompileEnumerableFactory(elementType));
        }

        return list;
    }

    private Func<Scope?, object> CompileEnumerableFactory(Type elementType)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);

        if (_aliases.TryGetValue(elementType, out var implementations))
        {
            var factories = implementations.Select(t => GetOrCreateFactory(t)).ToList();

            return scope =>
            {
                var list = (IList)Activator.CreateInstance(listType)!;
                foreach (var factory in factories)
                {
                    list.Add(factory(scope));
                }
                return list;
            };
        }

        if (_registrations.ContainsKey(elementType))
        {
            var factory = GetOrCreateFactory(elementType);
            return scope =>
            {
                var list = (IList)Activator.CreateInstance(listType)!;
                list.Add(factory(scope));
                return list;
            };
        }

        return _ => Activator.CreateInstance(listType)!;
    }

    #endregion

    #region Private Methods - Expression Building (Generic Path) with Tracking

    private (Expression<Func<T>>, HashSet<Type>) BuildTypedFactoryExpressionWithTracking<T>(Type type, TypeRegistration registration)
    {
        var dependencies = new HashSet<Type>();
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        ConstructorInfo? bestCtor = null;
        int bestScore = -1;

        foreach (var ctor in constructors)
        {
            var score = ScoreConstructor(ctor, registration.Parameters);
            if (score > bestScore)
            {
                bestScore = score;
                bestCtor = ctor;
            }
        }

        if (bestCtor == null)
            throw new InvalidOperationException($"No suitable constructor found for type {type.FullName}");

        var parameters = bestCtor.GetParameters();
        var arguments = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            arguments[i] = BuildParameterExpressionTypedWithTracking(param, i, registration.Parameters, dependencies, new HashSet<Type>());
        }

        var newExpr = Expression.New(bestCtor, arguments);

        Expression body = typeof(T) == type
            ? newExpr
            : Expression.Convert(newExpr, typeof(T));

        return (Expression.Lambda<Func<T>>(body), dependencies);
    }

    private Expression BuildParameterExpressionTypedWithTracking(ParameterInfo param, int position, List<DIParameter> registeredParams, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        foreach (var rp in registeredParams)
        {
            if (rp.Matches(param.Name ?? "", position, param.ParameterType))
            {
                return Expression.Constant(rp.Value, param.ParameterType);
            }
        }

        if (_registrations.ContainsKey(param.ParameterType) || _aliases.ContainsKey(param.ParameterType))
        {
            dependencies.Add(param.ParameterType);
            return BuildInlineResolutionTypedWithTracking(param.ParameterType, dependencies, resolutionStack);
        }

        if (param.IsOptional)
        {
            return Expression.Constant(param.DefaultValue, param.ParameterType);
        }

        throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' of type {param.ParameterType.FullName}");
    }

    private Expression BuildInlineResolutionTypedWithTracking(Type type, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        if (!resolutionStack.Add(type))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {type.FullName}. " +
                $"Resolution chain: {string.Join(" -> ", resolutionStack.Select(t => t.Name))} -> {type.Name}");
        }

        try
        {
            Type resolveType = type;
            if (_aliases.TryGetValue(type, out var implementations))
            {
                if (implementations.Count > 0)
                    resolveType = implementations[0];
                // Track the alias as a dependency too
                dependencies.Add(type);
            }

            if (_singletonInstances.TryGetValue(resolveType, out var singleton))
            {
                return Expression.Constant(singleton, type);
            }

            if (!_registrations.TryGetValue(resolveType, out var registration))
            {
                var ctor = resolveType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);

                if (ctor != null)
                {
                    var newExpr = Expression.New(ctor);
                    return type == resolveType ? newExpr : Expression.Convert(newExpr, type);
                }

                return BuildRuntimeResolutionFallback(type);
            }

            // Track the concrete type as a dependency
            dependencies.Add(resolveType);

            if (registration.Instance != null)
            {
                return Expression.Constant(registration.Instance, type);
            }

            var lifestyle = registration.Lifestyle;

            if (lifestyle == LifestyleType.Singleton)
            {
                return BuildInlineSingletonResolutionWithTracking(resolveType, type, registration, dependencies, resolutionStack);
            }

            return BuildInlineConstructionWithTracking(resolveType, type, registration, dependencies, resolutionStack);
        }
        finally
        {
            resolutionStack.Remove(type);
        }
    }

    private Expression BuildInlineSingletonResolutionWithTracking(Type resolveType, Type requestedType, TypeRegistration registration, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        // If singleton already exists, embed it directly as a constant
        if (_singletonInstances.TryGetValue(resolveType, out var existingSingleton))
        {
            return Expression.Constant(existingSingleton, requestedType);
        }

        // In eager mode, create the singleton now and embed as constant
        if (EagerCompilation)
        {
            var constructionExpr = BuildInlineConstructionWithTracking(resolveType, resolveType, registration, dependencies, resolutionStack);
            var factoryLambda = Expression.Lambda(constructionExpr);
            var compiledFactory = factoryLambda.Compile();

            // Create the singleton now during compilation
            var singleton = compiledFactory.DynamicInvoke();
            _singletonInstances.TryAdd(resolveType, singleton!);

            // Embed the singleton instance directly - no runtime lookup needed!
            return Expression.Constant(singleton, requestedType);
        }

        // In lazy mode, use GetOrCreateSingletonInline at runtime
        var innerExpr = BuildInlineConstructionWithTracking(resolveType, typeof(object), registration, dependencies, resolutionStack);
        var innerLambda = Expression.Lambda<Func<object>>(innerExpr);
        var innerCompiled = innerLambda.Compile();

        var method = typeof(ActivationBuilder)
            .GetMethod(nameof(GetOrCreateSingletonInline), BindingFlags.NonPublic | BindingFlags.Instance)!;

        var callExpr = Expression.Call(
            Expression.Constant(this),
            method,
            Expression.Constant(resolveType),
            Expression.Constant(innerCompiled));

        return requestedType == typeof(object)
            ? callExpr
            : Expression.Convert(callExpr, requestedType);
    }

    private Expression BuildInlineConstructionWithTracking(Type resolveType, Type requestedType, TypeRegistration registration, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        var constructors = resolveType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        ConstructorInfo? bestCtor = null;
        int bestScore = -1;

        foreach (var ctor in constructors)
        {
            var score = ScoreConstructor(ctor, registration.Parameters);
            if (score > bestScore)
            {
                bestScore = score;
                bestCtor = ctor;
            }
        }

        if (bestCtor == null)
            throw new InvalidOperationException($"No suitable constructor found for type {resolveType.FullName}");

        var parameters = bestCtor.GetParameters();
        var arguments = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            arguments[i] = BuildParameterExpressionTypedInlineWithTracking(param, i, registration.Parameters, dependencies, resolutionStack);
        }

        var newExpr = Expression.New(bestCtor, arguments);

        return requestedType == resolveType
            ? newExpr
            : Expression.Convert(newExpr, requestedType);
    }

    private Expression BuildParameterExpressionTypedInlineWithTracking(ParameterInfo param, int position, List<DIParameter> registeredParams, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        foreach (var rp in registeredParams)
        {
            if (rp.Matches(param.Name ?? "", position, param.ParameterType))
            {
                return Expression.Constant(rp.Value, param.ParameterType);
            }
        }

        if (_registrations.ContainsKey(param.ParameterType) || _aliases.ContainsKey(param.ParameterType))
        {
            dependencies.Add(param.ParameterType);
            return BuildInlineResolutionTypedWithTracking(param.ParameterType, dependencies, resolutionStack);
        }

        if (param.IsOptional)
        {
            return Expression.Constant(param.DefaultValue, param.ParameterType);
        }

        throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' of type {param.ParameterType.FullName}");
    }

    private Expression BuildRuntimeResolutionFallback(Type type)
    {
        var method = typeof(ActivationBuilder)
            .GetMethod(nameof(ResolveInlineTyped), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(type);

        return Expression.Call(Expression.Constant(this), method);
    }

    private T ResolveInlineTyped<T>()
    {
        if (_genericFactoryCache.TryGetValue(typeof(T), out var cached))
        {
            return ((Func<T>)cached)();
        }
        return LocateAndCacheGeneric<T>();
    }

    private object GetOrCreateSingletonInline(Type type, Delegate factory)
    {
        if (_singletonInstances.TryGetValue(type, out var existing))
        {
            return existing;
        }

        lock (_singletonInstances)
        {
            if (_singletonInstances.TryGetValue(type, out existing))
            {
                return existing;
            }

            var instance = ((Func<object>)factory)();
            _singletonInstances.TryAdd(type, instance);
            return instance;
        }
    }

    #endregion

    #region Private Methods - Expression Building (Non-Generic Path) with Tracking

    private (Expression<Func<Scope?, object>>, HashSet<Type>) BuildFactoryExpressionWithTracking(Type type, TypeRegistration registration)
    {
        var dependencies = new HashSet<Type>();
        var scopeParam = Expression.Parameter(typeof(Scope), "scope");
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        ConstructorInfo? bestCtor = null;
        int bestScore = -1;

        foreach (var ctor in constructors)
        {
            var score = ScoreConstructor(ctor, registration.Parameters);
            if (score > bestScore)
            {
                bestScore = score;
                bestCtor = ctor;
            }
        }

        if (bestCtor == null)
            throw new InvalidOperationException($"No suitable constructor found for type {type.FullName}");

        var parameters = bestCtor.GetParameters();
        var arguments = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            arguments[i] = BuildParameterExpressionWithTracking(param, i, registration.Parameters, scopeParam, dependencies, new HashSet<Type>());
        }

        var newExpr = Expression.New(bestCtor, arguments);
        var convertExpr = Expression.Convert(newExpr, typeof(object));

        return (Expression.Lambda<Func<Scope?, object>>(convertExpr, scopeParam), dependencies);
    }

    private Expression BuildParameterExpressionWithTracking(ParameterInfo param, int position, List<DIParameter> registeredParams, ParameterExpression scopeParam, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        foreach (var rp in registeredParams)
        {
            if (rp.Matches(param.Name ?? "", position, param.ParameterType))
            {
                return Expression.Constant(rp.Value, param.ParameterType);
            }
        }

        if (_registrations.ContainsKey(param.ParameterType) || _aliases.ContainsKey(param.ParameterType))
        {
            dependencies.Add(param.ParameterType);
            return BuildInlineResolutionExpressionWithTracking(param.ParameterType, scopeParam, dependencies, resolutionStack);
        }

        if (param.IsOptional)
        {
            return Expression.Constant(param.DefaultValue, param.ParameterType);
        }

        throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' of type {param.ParameterType.FullName}");
    }

    private Expression BuildInlineResolutionExpressionWithTracking(Type type, ParameterExpression scopeParam, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        if (!resolutionStack.Add(type))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {type.FullName}. " +
                $"Resolution chain: {string.Join(" -> ", resolutionStack.Select(t => t.Name))} -> {type.Name}");
        }

        try
        {
            Type resolveType = type;
            if (_aliases.TryGetValue(type, out var implementations))
            {
                if (implementations.Count > 0)
                    resolveType = implementations[0];
                dependencies.Add(type);
            }

            if (_singletonInstances.TryGetValue(resolveType, out var singleton))
            {
                return Expression.Constant(singleton, type);
            }

            if (!_registrations.TryGetValue(resolveType, out var registration))
            {
                var ctor = resolveType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);

                if (ctor != null)
                {
                    var newExpr = Expression.New(ctor);
                    return type == resolveType ? newExpr : Expression.Convert(newExpr, type);
                }

                return BuildRuntimeResolutionFallbackNonGeneric(type, scopeParam);
            }

            dependencies.Add(resolveType);

            if (registration.Instance != null)
            {
                return Expression.Constant(registration.Instance, type);
            }

            var lifestyle = registration.Lifestyle;

            if (lifestyle == LifestyleType.Singleton)
            {
                return BuildInlineSingletonResolutionNonGenericWithTracking(resolveType, type, registration, scopeParam, dependencies, resolutionStack);
            }

            if (lifestyle == LifestyleType.Scoped)
            {
                return BuildInlineScopedResolutionWithTracking(resolveType, type, registration, scopeParam, dependencies, resolutionStack);
            }

            return BuildInlineConstructionNonGenericWithTracking(resolveType, type, registration, scopeParam, dependencies, resolutionStack);
        }
        finally
        {
            resolutionStack.Remove(type);
        }
    }

    private Expression BuildInlineSingletonResolutionNonGenericWithTracking(Type resolveType, Type requestedType, TypeRegistration registration, ParameterExpression scopeParam, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        // If singleton already exists, embed it directly as a constant
        if (_singletonInstances.TryGetValue(resolveType, out var existingSingleton))
        {
            return requestedType == typeof(object)
                ? Expression.Constant(existingSingleton)
                : Expression.Constant(existingSingleton, requestedType);
        }

        // In eager mode, create the singleton now and embed as constant
        if (EagerCompilation)
        {
            var constructionExpr = BuildInlineConstructionNonGenericWithTracking(resolveType, typeof(object), registration, scopeParam, dependencies, resolutionStack);
            var factoryLambda = Expression.Lambda<Func<object>>(constructionExpr);
            var compiledFactory = factoryLambda.Compile();

            // Create the singleton now during compilation
            var singleton = compiledFactory();
            _singletonInstances.TryAdd(resolveType, singleton!);

            // Embed the singleton instance directly - no runtime lookup needed!
            return requestedType == typeof(object)
                ? Expression.Constant(singleton)
                : Expression.Constant(singleton, requestedType);
        }

        // In lazy mode, use GetOrCreateSingletonInline at runtime
        var innerExpr = BuildInlineConstructionNonGenericWithTracking(resolveType, typeof(object), registration, scopeParam, dependencies, resolutionStack);
        var innerLambda = Expression.Lambda<Func<object>>(innerExpr);
        var innerCompiled = innerLambda.Compile();

        var method = typeof(ActivationBuilder)
            .GetMethod(nameof(GetOrCreateSingletonInline), BindingFlags.NonPublic | BindingFlags.Instance)!;

        var callExpr = Expression.Call(
            Expression.Constant(this),
            method,
            Expression.Constant(resolveType),
            Expression.Constant(innerCompiled));

        return requestedType == typeof(object)
            ? callExpr
            : Expression.Convert(callExpr, requestedType);
    }

    private Expression BuildInlineScopedResolutionWithTracking(Type resolveType, Type requestedType, TypeRegistration registration, ParameterExpression scopeParam, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        var constructionExpr = BuildInlineConstructionNonGenericWithTracking(resolveType, typeof(object), registration, scopeParam, dependencies, resolutionStack);
        var factoryLambda = Expression.Lambda<Func<object>>(constructionExpr);
        var compiledFactory = factoryLambda.Compile();

        var method = typeof(ActivationBuilder)
            .GetMethod(nameof(GetOrCreateScopedInline), BindingFlags.NonPublic | BindingFlags.Instance)!;

        var callExpr = Expression.Call(
            Expression.Constant(this),
            method,
            Expression.Constant(resolveType),
            scopeParam,
            Expression.Constant(compiledFactory));

        return requestedType == typeof(object)
            ? callExpr
            : Expression.Convert(callExpr, requestedType);
    }

    private Expression BuildInlineConstructionNonGenericWithTracking(Type resolveType, Type requestedType, TypeRegistration registration, ParameterExpression scopeParam, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        var constructors = resolveType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        ConstructorInfo? bestCtor = null;
        int bestScore = -1;

        foreach (var ctor in constructors)
        {
            var score = ScoreConstructor(ctor, registration.Parameters);
            if (score > bestScore)
            {
                bestScore = score;
                bestCtor = ctor;
            }
        }

        if (bestCtor == null)
            throw new InvalidOperationException($"No suitable constructor found for type {resolveType.FullName}");

        var parameters = bestCtor.GetParameters();
        var arguments = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            arguments[i] = BuildParameterExpressionInlineWithTracking(param, i, registration.Parameters, scopeParam, dependencies, resolutionStack);
        }

        var newExpr = Expression.New(bestCtor, arguments);

        return requestedType == typeof(object)
            ? Expression.Convert(newExpr, typeof(object))
            : (requestedType == resolveType ? newExpr : Expression.Convert(newExpr, requestedType));
    }

    private Expression BuildParameterExpressionInlineWithTracking(ParameterInfo param, int position, List<DIParameter> registeredParams, ParameterExpression scopeParam, HashSet<Type> dependencies, HashSet<Type> resolutionStack)
    {
        foreach (var rp in registeredParams)
        {
            if (rp.Matches(param.Name ?? "", position, param.ParameterType))
            {
                return Expression.Constant(rp.Value, param.ParameterType);
            }
        }

        if (_registrations.ContainsKey(param.ParameterType) || _aliases.ContainsKey(param.ParameterType))
        {
            dependencies.Add(param.ParameterType);
            return BuildInlineResolutionExpressionWithTracking(param.ParameterType, scopeParam, dependencies, resolutionStack);
        }

        if (param.IsOptional)
        {
            return Expression.Constant(param.DefaultValue, param.ParameterType);
        }

        throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' of type {param.ParameterType.FullName}");
    }

    private Expression BuildRuntimeResolutionFallbackNonGeneric(Type type, ParameterExpression scopeParam)
    {
        var resolveMethod = typeof(ActivationBuilder).GetMethod(nameof(ResolveInline), BindingFlags.NonPublic | BindingFlags.Instance)!;
        var thisExpr = Expression.Constant(this);
        var typeExpr = Expression.Constant(type);

        var callExpr = Expression.Call(thisExpr, resolveMethod, typeExpr, scopeParam);
        return Expression.Convert(callExpr, type);
    }

    private object ResolveInline(Type type, Scope? scope)
    {
        if (_factoryCache.TryGetValue(type, out var factory))
        {
            return factory(scope);
        }
        return LocateInternalByType(type, scope);
    }

    private object GetOrCreateScopedInline(Type type, Scope? scope, Func<object> factory)
    {
        if (scope == null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve scoped service '{type.FullName}' from the root container. " +
                "Use container.CreateScope() and resolve from the scope instead.");
        }

        return scope.GetOrAddScoped(type, factory);
    }

    #endregion

    #region Private Methods - Constructor Scoring & Instance Creation

    private int ScoreConstructor(ConstructorInfo ctor, List<DIParameter> registeredParams)
    {
        var parameters = ctor.GetParameters();
        var score = 0;

        foreach (var param in parameters)
        {
            var canSatisfy = false;

            foreach (var rp in registeredParams)
            {
                if (rp.Matches(param.Name ?? "", param.Position, param.ParameterType))
                {
                    canSatisfy = true;
                    score += 10;
                    break;
                }
            }

            if (!canSatisfy)
            {
                if (_registrations.ContainsKey(param.ParameterType) || _aliases.ContainsKey(param.ParameterType))
                {
                    canSatisfy = true;
                    score += 5;
                }
                else if (param.IsOptional)
                {
                    canSatisfy = true;
                    score += 1;
                }
            }

            if (!canSatisfy) return -1;
        }

        return score;
    }

    private bool TryCreateInstance(ConstructorInfo ctor, TypeRegistration registration, DIParameter[] runtimeParams, Scope? scope, out object instance)
    {
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];
        var registeredParams = registration.Parameters;

        Span<bool> usedRegistered = stackalloc bool[registeredParams.Count];
        Span<bool> usedRuntime = stackalloc bool[runtimeParams.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var found = false;

            for (var j = 0; j < registeredParams.Count; j++)
            {
                if (usedRegistered[j]) continue;

                if (registeredParams[j].Matches(param.Name ?? "", param.Position, param.ParameterType))
                {
                    args[i] = registeredParams[j].Value;
                    usedRegistered[j] = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                for (var j = 0; j < runtimeParams.Length; j++)
                {
                    if (usedRuntime[j]) continue;

                    if (runtimeParams[j].Matches(param.Name ?? "", param.Position, param.ParameterType))
                    {
                        args[i] = runtimeParams[j].Value;
                        usedRuntime[j] = true;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                if (_registrations.ContainsKey(param.ParameterType) || _aliases.ContainsKey(param.ParameterType))
                {
                    args[i] = LocateWithScope(param.ParameterType, scope, Array.Empty<DIParameter>());
                    found = true;
                }
                else if (param.IsOptional)
                {
                    args[i] = param.DefaultValue;
                    found = true;
                }
            }

            if (!found)
            {
                instance = null!;
                return false;
            }
        }

        instance = ctor.Invoke(args);
        return true;
    }

    #endregion

    #region Private Methods - Alias Management

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAlias(Type aliasType, Type concreteType)
    {
        _aliases.AddOrUpdate(
            aliasType,
            _ => new List<Type>(2) { concreteType },
            (_, list) => { if (!list.Contains(concreteType)) list.Add(concreteType); return list; });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAliasFast(Type aliasType, Type concreteType)
    {
        if (_aliases.TryGetValue(aliasType, out var list))
        {
            if (!list.Contains(concreteType))
                list.Add(concreteType);
        }
        else
        {
            _aliases[aliasType] = new List<Type>(2) { concreteType };
        }
    }

    #endregion
}

#region Supporting Types

/// <summary>
/// Lightweight registration metadata - no compilation during registration
/// </summary>
internal readonly struct TypeRegistration
{
    public readonly Type Type;
    public readonly LifestyleType Lifestyle;
    public readonly List<DIParameter> Parameters;
    public readonly object? Instance;

    public TypeRegistration(Type type, LifestyleType lifestyle, List<DIParameter> parameters, object? instance)
    {
        Type = type;
        Lifestyle = lifestyle;
        Parameters = parameters;
        Instance = instance;
    }
}

#endregion
