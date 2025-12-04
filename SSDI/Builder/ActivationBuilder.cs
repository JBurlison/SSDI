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
/// </summary>
public class ActivationBuilder
{
    // Registration metadata (lightweight - no compilation)
    private readonly ConcurrentDictionary<Type, TypeRegistration> _registrations = new();

    // Alias to concrete type mapping (interface -> implementation)
    private readonly ConcurrentDictionary<Type, List<Type>> _aliases = new();

    // Pre-compiled factory cache for non-generic path
    private readonly ConcurrentDictionary<Type, Func<Scope?, object>> _factoryCache = new();

    // Generic factory cache - stores Func<T> for each type T (avoids boxing)
    private readonly ConcurrentDictionary<Type, Delegate> _genericFactoryCache = new();

    // Singleton instances
    private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();

    // Enumerable factory cache
    private readonly ConcurrentDictionary<Type, Func<Scope?, object>> _enumerableFactoryCache = new();

    // Container ID for static resolver binding
    private static int _nextContainerId;
    internal readonly int ContainerId = Interlocked.Increment(ref _nextContainerId);

    // Static resolver registry - maps container ID to container instance
    private static readonly ConcurrentDictionary<int, ActivationBuilder> _containers = new();

    /// <summary>
    /// Occurs when a type is unregistered from the container.
    /// </summary>
    public event EventHandler<UnregisteredEventArgs>? Unregistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivationBuilder"/> class.
    /// </summary>
    public ActivationBuilder()
    {
        _containers[ContainerId] = this;
    }

    internal void Add(ExportRegistration reg)
    {
        foreach (var exportRegistration in reg.Registrations)
        {
            var exportedType = exportRegistration.ExportedType;
            var fluentReg = exportRegistration.FluentExportRegistration;
            var lifestyle = fluentReg.LifestyleValue;

            // Get parameters only if they exist (avoids List allocation)
            var parameters = fluentReg.HasParameters ? fluentReg.ParametersInternal : EmptyParameters;

            // For pre-built instances, store directly
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
                continue;
            }

            _registrations[exportedType] = new TypeRegistration(exportedType, lifestyle, parameters, null);

            if (fluentReg.HasAlias)
            {
                foreach (var aliasType in fluentReg.Alias)
                {
                    AddAliasFast(aliasType, exportedType);
                    // Only invalidate cache if this is a re-registration (cache already exists)
                    if (_factoryCache.ContainsKey(aliasType))
                    {
                        _factoryCache.TryRemove(aliasType, out _);
                        _genericFactoryCache.TryRemove(aliasType, out _);
                    }
                }
            }

            // Only invalidate cache if this is a re-registration
            if (_factoryCache.ContainsKey(exportedType))
            {
                _factoryCache.TryRemove(exportedType, out _);
                _genericFactoryCache.TryRemove(exportedType, out _);
            }
        }
    }

    private static readonly List<DIParameter> EmptyParameters = new(0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAlias(Type aliasType, Type concreteType)
    {
        _aliases.AddOrUpdate(
            aliasType,
            _ => new List<Type>(2) { concreteType },
            (_, list) => { lock (list) { if (!list.Contains(concreteType)) list.Add(concreteType); } return list; });
    }

    /// <summary>
    /// Fast path for adding aliases during registration (single-threaded)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddAliasFast(Type aliasType, Type concreteType)
    {
        if (_aliases.TryGetValue(aliasType, out var list))
        {
            // Common case: alias already exists
            if (!list.Contains(concreteType))
                list.Add(concreteType);
        }
        else
        {
            // New alias - use small initial capacity
            _aliases[aliasType] = new List<Type>(2) { concreteType };
        }
    }

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

        if (removeFromAliases)
        {
            foreach (var kvp in _aliases)
            {
                lock (kvp.Value)
                {
                    if (kvp.Value.Remove(type))
                    {
                        _factoryCache.TryRemove(kvp.Key, out _);
                        _genericFactoryCache.TryRemove(kvp.Key, out _);
                        _enumerableFactoryCache.TryRemove(typeof(IEnumerable<>).MakeGenericType(kvp.Key), out _);
                    }
                }
            }
        }

        if (removed)
        {
            Unregistered?.Invoke(this, new UnregisteredEventArgs(type, removedInstance, wasDisposed));
        }

        return removed;
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
        if (!_aliases.TryRemove(aliasType, out var implementations)) return 0;

        var count = 0;
        List<Type> implCopy;
        lock (implementations) { implCopy = new List<Type>(implementations); }

        foreach (var implType in implCopy)
        {
            if (Unregister(implType, removeFromAliases: false)) count++;
        }

        _factoryCache.TryRemove(aliasType, out _);
        _genericFactoryCache.TryRemove(aliasType, out _);
        _enumerableFactoryCache.TryRemove(typeof(IEnumerable<>).MakeGenericType(aliasType), out _);

        return count;
    }

    /// <summary>
    /// Checks if a type is registered in the container.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>True if the type is registered; otherwise, false.</returns>
    public bool IsRegistered<T>() => IsRegistered(typeof(T));

    /// <summary>
    /// Checks if a type is registered in the container.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is registered; otherwise, false.</returns>
    public bool IsRegistered(Type type) =>
        _registrations.ContainsKey(type) ||
        _aliases.ContainsKey(type) ||
        _singletonInstances.ContainsKey(type);

    /// <summary>
    /// Ultra-fast generic locate - uses cached Func&lt;T&gt; to avoid boxing
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>()
    {
        // Fast path: check generic factory cache (no boxing!)
        if (_genericFactoryCache.TryGetValue(typeof(T), out var cached))
        {
            return ((Func<T>)cached)();
        }

        return LocateAndCacheGeneric<T>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private T LocateAndCacheGeneric<T>()
    {
        // Build and cache a Func<T> for this type
        var factory = BuildGenericFactory<T>();
        _genericFactoryCache.TryAdd(typeof(T), factory);
        return factory();
    }

    private Func<T> BuildGenericFactory<T>()
    {
        var type = typeof(T);

        // Check for IEnumerable<T> - must handle before alias check
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return BuildEnumerableFactory<T>(type);
        }

        // Check for alias first
        Type? concreteType = null;
        if (_aliases.TryGetValue(type, out var implementations))
        {
            lock (implementations)
            {
                if (implementations.Count > 0)
                    concreteType = implementations[0];
            }
        }

        var resolveType = concreteType ?? type;

        // Check for pre-built singleton
        if (_singletonInstances.TryGetValue(resolveType, out var singleton))
        {
            var s = (T)singleton;
            return () => s;
        }

        if (!_registrations.TryGetValue(resolveType, out var registration))
        {
            // Fallback: try parameterless constructor
            return BuildFallbackFactory<T>(resolveType);
        }

        if (registration.Instance != null)
        {
            var inst = (T)registration.Instance;
            return () => inst;
        }

        var lifestyle = registration.Lifestyle;

        // Compile the factory
        var factoryExpr = BuildTypedFactoryExpression<T>(resolveType, registration);
        var compiled = factoryExpr.Compile();

        return lifestyle switch
        {
            LifestyleType.Singleton => CreateSingletonFactory<T>(resolveType, compiled),
            LifestyleType.Scoped => throw new InvalidOperationException(
                $"Cannot resolve scoped service '{type.FullName}' from the root container. " +
                "Use container.CreateScope() and resolve from the scope instead."),
            _ => compiled // Transient - direct factory call
        };
    }

    private Func<T> CreateSingletonFactory<T>(Type type, Func<T> innerFactory)
    {
        T? instance = default;
        var syncRoot = new object();
        var created = false;

        return () =>
        {
            if (created) return instance!;

            lock (syncRoot)
            {
                if (created) return instance!;
                instance = innerFactory();
                _singletonInstances.TryAdd(type, instance!);
                created = true;
                return instance;
            }
        };
    }

    private Expression<Func<T>> BuildTypedFactoryExpression<T>(Type type, TypeRegistration registration)
    {
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
            arguments[i] = BuildParameterExpressionTyped(param, i, registration.Parameters);
        }

        var newExpr = Expression.New(bestCtor, arguments);

        // If T is the same as the type, return directly; otherwise cast
        Expression body = typeof(T) == type
            ? newExpr
            : Expression.Convert(newExpr, typeof(T));

        return Expression.Lambda<Func<T>>(body);
    }

    private Expression BuildParameterExpressionTyped(ParameterInfo param, int position, List<DIParameter> registeredParams)
    {
        // Check registered parameters first
        foreach (var rp in registeredParams)
        {
            if (rp.Matches(param.Name ?? "", position, param.ParameterType))
            {
                return Expression.Constant(rp.Value, param.ParameterType);
            }
        }

        // Try to resolve from container
        if (_registrations.ContainsKey(param.ParameterType) || _aliases.ContainsKey(param.ParameterType))
        {
            return BuildInlineResolutionTyped(param.ParameterType);
        }

        // Check for optional parameter
        if (param.IsOptional)
        {
            return Expression.Constant(param.DefaultValue, param.ParameterType);
        }

        throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' of type {param.ParameterType.FullName}");
    }

    private Expression BuildInlineResolutionTyped(Type type)
    {
        // Generate inline call: this.ResolveInlineTyped<T>()
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
            List<Type> implCopy;
            lock (implementations) { implCopy = new List<Type>(implementations); }

            if (implCopy.Count == 0)
            {
                // No implementations registered - return empty list
                return () => (T)Activator.CreateInstance(listType)!;
            }

            var factories = implCopy.Select(t => GetOrCreateFactory(t)).ToList();
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

        // No registrations - return empty list
        return () => (T)Activator.CreateInstance(listType)!;
    }

    // ==================== Non-generic path (for runtime Type resolution) ====================

    /// <summary>
    /// Locates and returns an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>An instance of the specified type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Locate(Type type) => Locate(type, Array.Empty<DIParameter>());

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
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <param name="parameters">Struct-based parameters.</param>
    /// <returns>An instance of the specified type.</returns>
    public object Locate(Type type, params DIParameter[] parameters)
    {
        return LocateWithScope(type, null, parameters);
    }

    internal object LocateWithScope(Type type, Scope? scope, DIParameter[] parameters)
    {
        // Check for IEnumerable<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return ResolveEnumerable(type, scope, parameters);
        }

        // Fast path for no runtime parameters
        if (parameters.Length == 0)
        {
            if (_factoryCache.TryGetValue(type, out var factory))
            {
                return factory(scope);
            }
            return LocateInternal(type, scope);
        }

        // Slow path with runtime parameters
        return LocateWithRuntimeParams(type, scope, parameters);
    }

    private object LocateInternal(Type type, Scope? scope)
    {
        // Check for alias first
        if (_aliases.TryGetValue(type, out var implementations))
        {
            Type? concreteType = null;
            lock (implementations)
            {
                if (implementations.Count > 0)
                    concreteType = implementations[0];
            }

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
        return _factoryCache.GetOrAdd(type, t => CompileFactory(t));
    }

    private Func<Scope?, object> CompileFactory(Type type)
    {
        if (_singletonInstances.TryGetValue(type, out var existingInstance))
        {
            return _ => existingInstance;
        }

        if (!_registrations.TryGetValue(type, out var registration))
        {
            return CompileFallbackFactory(type);
        }

        if (registration.Instance != null)
        {
            var instance = registration.Instance;
            return _ => instance;
        }

        var lifestyle = registration.Lifestyle;
        var factoryExpr = BuildFactoryExpression(type, registration);
        var compiledFactory = factoryExpr.Compile();

        return lifestyle switch
        {
            LifestyleType.Singleton => CreateSingletonFactoryNonGeneric(type, compiledFactory),
            LifestyleType.Scoped => CreateScopedFactory(type, compiledFactory),
            _ => compiledFactory
        };
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

    private Expression<Func<Scope?, object>> BuildFactoryExpression(Type type, TypeRegistration registration)
    {
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
            arguments[i] = BuildParameterExpression(param, i, registration.Parameters, scopeParam);
        }

        var newExpr = Expression.New(bestCtor, arguments);
        var convertExpr = Expression.Convert(newExpr, typeof(object));

        return Expression.Lambda<Func<Scope?, object>>(convertExpr, scopeParam);
    }

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

    private Expression BuildParameterExpression(ParameterInfo param, int position, List<DIParameter> registeredParams, ParameterExpression scopeParam)
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
            return BuildInlineResolutionExpression(param.ParameterType, scopeParam);
        }

        if (param.IsOptional)
        {
            return Expression.Constant(param.DefaultValue, param.ParameterType);
        }

        throw new InvalidOperationException($"Cannot resolve parameter '{param.Name}' of type {param.ParameterType.FullName}");
    }

    private Expression BuildInlineResolutionExpression(Type type, ParameterExpression scopeParam)
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
        return LocateInternal(type, scope);
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
            List<Type> implCopy;
            lock (implementations) { implCopy = new List<Type>(implementations); }

            foreach (var implType in implCopy)
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
                        list.Add(LocateInternal(implType, scope));
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
                list.Add(LocateInternal(elementType, scope));
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
            List<Func<Scope?, object>> factories;
            lock (implementations)
            {
                factories = implementations.Select(t => GetOrCreateFactory(t)).ToList();
            }

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

    private object LocateWithRuntimeParams(Type type, Scope? scope, DIParameter[] parameters)
    {
        Type resolveType = type;

        if (_aliases.TryGetValue(type, out var implementations))
        {
            lock (implementations)
            {
                if (implementations.Count > 0)
                    resolveType = implementations[0];
            }
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

    /// <summary>
    /// Creates a new scope for resolving scoped services.
    /// </summary>
    /// <returns>A new <see cref="IScope"/> instance.</returns>
    public IScope CreateScope() => new Scope(this);
}

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
