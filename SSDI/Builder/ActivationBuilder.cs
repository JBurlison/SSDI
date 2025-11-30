using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using SSDI.Parameters;
using SSDI.Registration;

namespace SSDI.Builder;

/// <summary>
/// Provides the core activation and resolution logic for the dependency injection container.
/// Handles constructor caching, lifetime management, and parameter resolution.
/// </summary>
/// <remarks>
/// <para>
/// This class is the base class for <see cref="SSDI.DependencyInjectionContainer"/> and provides
/// all the <c>Locate</c> methods for resolving dependencies.
/// </para>
/// <para>
/// Performance optimizations include:
/// <list type="bullet">
///   <item><description>Lock-free reads using ImmutableHashSet for aliases</description></item>
///   <item><description>ArrayPool for parameter arrays to reduce allocations</description></item>
///   <item><description>Stack allocation for small consumed tracking arrays</description></item>
///   <item><description>Struct-based parameters to avoid virtual dispatch</description></item>
///   <item><description>Flattened constructor storage for faster iteration</description></item>
/// </list>
/// </para>
/// </remarks>
public class ActivationBuilder
{
    // Flattened constructor storage - CachedConstructor[] instead of List<CachedConstructor[]>
    private readonly ConcurrentDictionary<Type, CachedConstructor[]> _constructors = new();
    private readonly ConcurrentDictionary<Type, LifestyleType> _lifetimes = new();
    private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();
    
    // Lock-free alias storage using ImmutableHashSet
    private readonly ConcurrentDictionary<Type, ImmutableHashSet<Type>> _alias = new();
    
    // List factory cache
    private readonly ConcurrentDictionary<Type, Func<IList>> _listFactories = new();
    
    // Enumerable type cache - stores element type or null if not enumerable
    private readonly ConcurrentDictionary<Type, Type?> _enumerableElementTypeCache = new();

    // Reusable empty arrays to avoid allocations
    private static readonly DIParameter[] EmptyParameters = Array.Empty<DIParameter>();
    private static readonly object?[] EmptyObjectArray = Array.Empty<object?>();
    
    // ArrayPool for parameter arrays
    private static readonly ArrayPool<object?> ParamPool = ArrayPool<object?>.Shared;

    internal void Add(ExportRegistration reg)
    {
        foreach (var exportRegistration in reg.Registrations)
        {
            var exportedType = exportRegistration.ExportedType;
            var lifestyle = exportRegistration.FluentExportRegistration.Lifestyle.Lifestyle;
            
            _lifetimes[exportedType] = lifestyle;

            foreach (var aliasType in exportRegistration.FluentExportRegistration.Alias)
            {
                // Lock-free update using ImmutableHashSet
                _alias.AddOrUpdate(
                    aliasType,
                    _ => ImmutableHashSet.Create(exportedType),
                    (_, existing) => existing.Add(exportedType));
                
                _lifetimes[aliasType] = lifestyle;
            }

            if (lifestyle == LifestyleType.Singleton && exportRegistration.Instance is not null)
            {
                _singletonInstances[exportedType] = exportRegistration.Instance;
                continue;
            }

            // Build constructor array - merge with existing if any
            var ctorInfos = exportedType.GetConstructors();
            var newCtors = new CachedConstructor[ctorInfos.Length];
            
            for (var i = 0; i < ctorInfos.Length; i++)
            {
                newCtors[i] = new CachedConstructor(ctorInfos[i], exportRegistration.FluentExportRegistration.ParametersInternal);
            }

            _constructors.AddOrUpdate(
                exportedType,
                newCtors,
                (_, existing) =>
                {
                    // Merge existing constructors with new ones
                    var merged = new CachedConstructor[existing.Length + newCtors.Length];
                    existing.CopyTo(merged, 0);
                    newCtors.CopyTo(merged, existing.Length);
                    return merged;
                });
        }
    }

    /// <summary>
    /// Unregisters a type from the container.
    /// </summary>
    /// <typeparam name="T">The type to unregister.</typeparam>
    /// <param name="removeFromAliases">If true, also removes this type from any alias (interface) registrations it belongs to.</param>
    /// <returns>True if the type was found and removed; otherwise, false.</returns>
    /// <remarks>
    /// If the type was registered as a singleton and has already been instantiated, the cached instance will be removed.
    /// If the instance implements <see cref="IDisposable"/>, it will be disposed.
    /// </remarks>
    /// <example>
    /// <code>
    /// container.Configure(c => c.Export&lt;OldService&gt;().As&lt;IService&gt;());
    /// 
    /// // Later, unregister OldService and remove it from IService alias
    /// container.Unregister&lt;OldService&gt;(removeFromAliases: true);
    /// 
    /// // Register a new implementation
    /// container.Configure(c => c.Export&lt;NewService&gt;().As&lt;IService&gt;());
    /// </code>
    /// </example>
    public bool Unregister<T>(bool removeFromAliases = true) => Unregister(typeof(T), removeFromAliases);

    /// <summary>
    /// Unregisters a type from the container.
    /// </summary>
    /// <param name="type">The type to unregister.</param>
    /// <param name="removeFromAliases">If true, also removes this type from any alias (interface) registrations it belongs to.</param>
    /// <returns>True if the type was found and removed; otherwise, false.</returns>
    /// <remarks>
    /// If the type was registered as a singleton and has already been instantiated, the cached instance will be removed.
    /// If the instance implements <see cref="IDisposable"/>, it will be disposed.
    /// </remarks>
    public bool Unregister(Type type, bool removeFromAliases = true)
    {
        var removed = false;

        // Remove constructors
        if (_constructors.TryRemove(type, out _))
        {
            removed = true;
        }

        // Remove lifetime registration
        _lifetimes.TryRemove(type, out _);

        // Remove singleton instance and dispose if applicable
        if (_singletonInstances.TryRemove(type, out var instance))
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
#if NET8_0_OR_GREATER
            else if (instance is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
#endif
            removed = true;
        }

        // Remove from all alias sets
        if (removeFromAliases)
        {
            foreach (var kvp in _alias)
            {
                if (kvp.Value.Contains(type))
                {
                    _alias.AddOrUpdate(
                        kvp.Key,
                        _ => ImmutableHashSet<Type>.Empty,
                        (_, existing) => existing.Remove(type));
                }
            }
        }

        return removed;
    }

    /// <summary>
    /// Unregisters all implementations registered under an alias (interface) type.
    /// </summary>
    /// <typeparam name="TAlias">The alias type (typically an interface) to unregister all implementations for.</typeparam>
    /// <returns>The number of implementations that were unregistered.</returns>
    /// <remarks>
    /// This method removes all concrete types that were registered with <c>.As&lt;TAlias&gt;()</c>.
    /// Each implementation's singleton instance (if any) will be disposed if it implements <see cref="IDisposable"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;AuthHandler&gt;().As&lt;IPacketHandler&gt;();
    ///     c.Export&lt;GameHandler&gt;().As&lt;IPacketHandler&gt;();
    ///     c.Export&lt;ChatHandler&gt;().As&lt;IPacketHandler&gt;();
    /// });
    /// 
    /// // Unregister all packet handlers
    /// int count = container.UnregisterAll&lt;IPacketHandler&gt;(); // Returns 3
    /// </code>
    /// </example>
    public int UnregisterAll<TAlias>() => UnregisterAll(typeof(TAlias));

    /// <summary>
    /// Unregisters all implementations registered under an alias (interface) type.
    /// </summary>
    /// <param name="aliasType">The alias type (typically an interface) to unregister all implementations for.</param>
    /// <returns>The number of implementations that were unregistered.</returns>
    /// <remarks>
    /// This method removes all concrete types that were registered with <c>.As(aliasType)</c>.
    /// Each implementation's singleton instance (if any) will be disposed if it implements <see cref="IDisposable"/>.
    /// </remarks>
    public int UnregisterAll(Type aliasType)
    {
        if (!_alias.TryRemove(aliasType, out var implementations))
        {
            return 0;
        }

        var count = 0;
        foreach (var implType in implementations)
        {
            if (Unregister(implType, removeFromAliases: false))
            {
                count++;
            }
        }

        // Also remove the alias lifetime
        _lifetimes.TryRemove(aliasType, out _);

        return count;
    }

    /// <summary>
    /// Checks if a type is registered in the container.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>True if the type is registered; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// if (container.IsRegistered&lt;ILogger&gt;())
    /// {
    ///     var logger = container.Locate&lt;ILogger&gt;();
    /// }
    /// </code>
    /// </example>
    public bool IsRegistered<T>() => IsRegistered(typeof(T));

    /// <summary>
    /// Checks if a type is registered in the container.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is registered; otherwise, false.</returns>
    public bool IsRegistered(Type type) => 
        _constructors.ContainsKey(type) || 
        _alias.ContainsKey(type) || 
        _singletonInstances.ContainsKey(type);

    /// <summary>
    /// Locates and returns an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code>
    /// var server = container.Locate&lt;TCPServer&gt;();
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>() => (T)Locate(typeof(T), EmptyParameters);

    /// <summary>
    /// Locates and returns an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>An instance of the specified type.</returns>
    /// <example>
    /// <code>
    /// var server = container.Locate(typeof(TCPServer));
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object Locate(Type type) => Locate(type, EmptyParameters);

    /// <summary>
    /// Locates and returns an instance with positional constructor parameters.
    /// Parameters are matched by their position in the constructor (0-based).
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">The positional parameter values, starting at position 0.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code>
    /// // For a constructor: TCPServer(string address, int port)
    /// var server = container.LocateWithPositionalParams&lt;TCPServer&gt;("127.0.0.1", 8080);
    /// </code>
    /// </example>
    public T LocateWithPositionalParams<T>(params object[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.Positional(i, parameters[i]);
        }
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with named constructor parameters.
    /// Parameters are matched by their name in the constructor.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Tuples of parameter names and values.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code>
    /// // For a constructor: TCPServer(string address, int port)
    /// var server = container.LocateWithNamedParameters&lt;TCPServer&gt;(
    ///     ("address", "127.0.0.1"), 
    ///     ("port", 8080)
    /// );
    /// </code>
    /// </example>
    public T LocateWithNamedParameters<T>(params (string name, object value)[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.Named(parameters[i].name, parameters[i].value);
        }
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with typed constructor parameters.
    /// Parameters are matched by their type.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">The parameter values to match by type.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code>
    /// // For a constructor: TCPServer(string address, int port)
    /// var server = container.LocateWithTypedParams&lt;TCPServer&gt;("127.0.0.1", 8080);
    /// </code>
    /// </example>
    public T LocateWithTypedParams<T>(params object[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.Typed(parameters[i]);
        }
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with custom DI parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Custom <see cref="IDIParameter"/> instances for parameter matching.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code>
    /// var server = container.Locate&lt;TCPServer&gt;(
    ///     new NamedParameter("address", "127.0.0.1"),
    ///     new PositionalParameter(1, 8080)
    /// );
    /// </code>
    /// </example>
    public T Locate<T>(params IDIParameter[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.FromLegacy(parameters[i]);
        }
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with a single positional parameter.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="position">The zero-based position of the parameter in the constructor.</param>
    /// <param name="value">The value for the parameter.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code>
    /// // For a constructor: TCPServer(string address, int port)
    /// var server = container.Locate&lt;TCPServer&gt;(0, "127.0.0.1");
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>(int position, object value)
    {
        var diParams = new DIParameter[1];
        diParams[0] = DIParameter.Positional(position, value);
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// Locates and returns an instance with custom DI parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Custom <see cref="IDIParameter"/> instances for parameter matching.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    /// <example>
    /// <code>
    /// var server = container.LocateWithParams&lt;TCPServer&gt;(
    ///     new TypedParameter("127.0.0.1"),
    ///     new TypedParameter(8080)
    /// );
    /// </code>
    /// </example>
    public T LocateWithParams<T>(params IDIParameter[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.FromLegacy(parameters[i]);
        }
        return (T)Locate(typeof(T), diParams);
    }

    /// <summary>
    /// High-performance locate with struct-based parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Struct-based parameters for optimal performance.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Locate<T>(params DIParameter[] parameters) => (T)Locate(typeof(T), parameters);

    /// <summary>
    /// Locates and returns an instance of the specified type with custom DI parameters.
    /// Also supports resolving <see cref="IEnumerable{T}"/> to get all registered implementations of an interface.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <param name="parameters">Custom <see cref="IDIParameter"/> instances for parameter matching.</param>
    /// <returns>An instance of the specified type, or an enumerable of instances if <paramref name="type"/> is <see cref="IEnumerable{T}"/>.</returns>
    /// <example>
    /// <code>
    /// // Locate a single instance
    /// var server = container.Locate(typeof(TCPServer), new NamedParameter("port", 8080));
    /// 
    /// // Locate all implementations of an interface
    /// var routes = container.Locate(typeof(IEnumerable&lt;IPacketRouter&gt;));
    /// </code>
    /// </example>
    public object Locate(Type type, params IDIParameter[] parameters)
    {
        var diParams = new DIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = DIParameter.FromLegacy(parameters[i]);
        }
        return Locate(type, diParams);
    }

    /// <summary>
    /// High-performance locate with struct-based parameters.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <param name="parameters">Struct-based parameters.</param>
    /// <returns>An instance of the specified type.</returns>
    public object Locate(Type type, params DIParameter[] parameters)
    {
        var elementType = GetEnumerableElementType(type);
        var isEnumerable = elementType is not null;
        var resolveType = isEnumerable ? elementType! : type;

        if (_alias.TryGetValue(resolveType, out var aliasSet))
        {
            if (isEnumerable)
            {
                var list = CreateListOfTType(resolveType);
                
                // ImmutableHashSet - no lock needed for iteration
                foreach (var aliasedType in aliasSet)
                {
                    if (LocateInternal(aliasedType, true, parameters) is IList listOfObj)
                    {
                        foreach (var o in listOfObj)
                            list.Add(o);
                    }
                }

                return list;
            }
            else
            {
                // Return first matching alias
                foreach (var aliasedType in aliasSet)
                {
                    return LocateInternal(aliasedType, false, parameters);
                }

                throw new InvalidOperationException($"Could not find a concrete type for interface {type.FullName}");
            }
        }

        return LocateInternal(resolveType, isEnumerable, parameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Type? GetEnumerableElementType(Type type)
    {
        return _enumerableElementTypeCache.GetOrAdd(type, static t =>
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return t.GetGenericArguments()[0];
            }
            return null;
        });
    }

    private object LocateInternal(Type type, bool isEnumerable, DIParameter[] parameters)
    {
        // Fast path: check singleton cache first
        if (_singletonInstances.TryGetValue(type, out var singletonInstance))
        {
            if (isEnumerable)
            {
                var singletonList = CreateListOfTType(type);
                singletonList.Add(singletonInstance);
                return singletonList;
            }
            return singletonInstance;
        }

        _lifetimes.TryGetValue(type, out var lifestyleType);

        if (_constructors.TryGetValue(type, out var constructors))
        {
            if (isEnumerable)
            {
                var list = CreateListOfTType(type);

                foreach (var ctor in constructors)
                {
                    if (ctor.CanSatisfy(parameters.Length) && TryActivate(ctor, type, lifestyleType, parameters, out var val))
                    {
                        list.Add(val);
                    }
                }

                return list;
            }
            else
            {
                foreach (var ctor in constructors)
                {
                    if (ctor.CanSatisfy(parameters.Length) && TryActivate(ctor, type, lifestyleType, parameters, out var val))
                    {
                        return val;
                    }
                }
            }
        }

        return isEnumerable ? CreateListOfTType(type) : Activator.CreateInstance(type)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IList CreateListOfTType(Type type)
    {
        var factory = _listFactories.GetOrAdd(type, static t =>
        {
            var listType = typeof(List<>).MakeGenericType(t);
            var ctor = listType.GetConstructor(Type.EmptyTypes)!;
            var lambda = Expression.Lambda<Func<IList>>(Expression.New(ctor));
            return lambda.Compile();
        });

        return factory();
    }

    private bool TryActivate(CachedConstructor c, Type type, LifestyleType lifestyleType, DIParameter[] runtimeParameters, out object val)
    {
        var paramCount = c.ParameterCount;
        
        // Fast path: parameterless constructor
        if (paramCount == 0)
        {
            var noParamsInstance = c.ConstructorFunc(EmptyObjectArray);

            if (noParamsInstance is null)
            {
                val = default!;
                return false;
            }

            if (lifestyleType == LifestyleType.Singleton)
            {
                _singletonInstances.TryAdd(type, noParamsInstance);
            }

            val = noParamsInstance;
            return true;
        }

        // Use ArrayPool for parameter array to reduce allocations
        var parameters = ParamPool.Rent(paramCount);
        
        try
        {
            var registeredParams = c.ParameterValues;
            var registeredCount = registeredParams.Length;
            var runtimeCount = runtimeParameters.Length;

            // Use stackalloc for consumed tracking when counts are small (typical case)
            Span<bool> consumedRegistered = registeredCount <= 16 
                ? stackalloc bool[registeredCount] 
                : new bool[registeredCount];
            
            Span<bool> consumedRuntime = runtimeCount <= 16 
                ? stackalloc bool[runtimeCount] 
                : new bool[runtimeCount];

            for (var i = 0; i < paramCount; i++)
            {
                var p = c.Parameters[i];
                var paramName = p.Name ?? "";
                var paramPosition = p.Position;
                var paramType = p.ParameterType;
                var foundParameter = false;

                // Check registered parameters first (struct iteration - no virtual dispatch)
                for (var j = 0; j < registeredCount; j++)
                {
                    if (consumedRegistered[j])
                        continue;

                    ref readonly var parameterValue = ref registeredParams[j];
                    if (parameterValue.Matches(paramName, paramPosition, paramType))
                    {
                        parameters[i] = parameterValue.Value;
                        foundParameter = true;
                        consumedRegistered[j] = true;
                        break;
                    }
                }

                if (!foundParameter)
                {
                    // Check runtime parameters (struct iteration - no virtual dispatch)
                    for (var j = 0; j < runtimeCount; j++)
                    {
                        if (consumedRuntime[j])
                            continue;

                        ref readonly var parameterValue = ref runtimeParameters[j];
                        if (parameterValue.Matches(paramName, paramPosition, paramType))
                        {
                            parameters[i] = parameterValue.Value;
                            foundParameter = true;
                            consumedRuntime[j] = true;
                            break;
                        }
                    }
                }

                if (foundParameter)
                    continue;

                // Try to resolve from container
                if (_constructors.ContainsKey(paramType) || _alias.ContainsKey(paramType))
                {
                    parameters[i] = Locate(paramType);
                    continue;
                }

                // Check for optional parameter
                if (p.IsOptional)
                {
                    parameters[i] = p.DefaultValue;
                    continue;
                }

                // Could not satisfy parameter
                val = default!;
                return false;
            }

            var instance = c.ConstructorFunc(parameters);

            if (instance is null)
            {
                val = default!;
                return false;
            }

            if (lifestyleType == LifestyleType.Singleton)
            {
                _singletonInstances.TryAdd(type, instance);
            }

            val = instance;
            return true;
        }
        finally
        {
            // Return to pool and clear to avoid holding references
            ParamPool.Return(parameters, clearArray: true);
        }
    }
}
