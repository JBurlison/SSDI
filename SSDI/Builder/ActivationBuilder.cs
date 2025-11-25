using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using SSDI.Parameters;
using SSDI.Registration;

namespace SSDI.Builder;

public class ActivationBuilder
{
    private readonly ConcurrentDictionary<Type, List<CachedConstructor[]>> _constructors = new();
    private readonly ConcurrentDictionary<Type, LifestyleType> _lifetimes = new();
    private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();
    private readonly ConcurrentDictionary<Type, HashSet<Type>> _alias = new();
    private readonly ConcurrentDictionary<Type, Func<IList>> _listFactories = new();
    private readonly ConcurrentDictionary<Type, bool> _isEnumerableCache = new();

    // Reusable empty array to avoid allocations
    private static readonly IDIParameter[] EmptyParameters = Array.Empty<IDIParameter>();
    private static readonly object?[] EmptyObjectArray = Array.Empty<object?>();

    internal void Add(ExportRegistration reg)
    {
        foreach (var exportRegistration in reg.Registrations)
        {
            _lifetimes[exportRegistration.ExportedType] = exportRegistration.FluentExportRegistration.Lifestyle.Lifestyle;

            foreach (var aliasType in exportRegistration.FluentExportRegistration.Alias)
            {
                var aliasSet = _alias.GetOrAdd(aliasType, _ => new HashSet<Type>());
                lock (aliasSet)
                {
                    aliasSet.Add(exportRegistration.ExportedType);
                }
                _lifetimes[aliasType] = exportRegistration.FluentExportRegistration.Lifestyle.Lifestyle;
            }

            if (exportRegistration.FluentExportRegistration.Lifestyle.Lifestyle == LifestyleType.Singleton &&
                exportRegistration.Instance is not null)
            {
                _singletonInstances[exportRegistration.ExportedType] = exportRegistration.Instance;
                continue;
            }

            // Order constructors by parameter count, descending so we get the lowest parameter count constructor first
            var constructors = _constructors.GetOrAdd(exportRegistration.ExportedType, _ => new List<CachedConstructor[]>());
            var ctorInfos = exportRegistration.ExportedType.GetConstructors();
            var cachedCtors = new CachedConstructor[ctorInfos.Length];
            for (var i = 0; i < ctorInfos.Length; i++)
            {
                cachedCtors[i] = new CachedConstructor(ctorInfos[i], exportRegistration.FluentExportRegistration.Parameters);
            }

            lock (constructors)
            {
                constructors.Add(cachedCtors);
            }
        }
    }

    public T Locate<T>() => (T)Locate(typeof(T));

    public object Locate(Type type) => Locate(type, EmptyParameters);

    public T LocateWithPositionalParams<T>(params object[] parameters)
    {
        var diParams = new IDIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = new PositionalParameter(i, parameters[i]);
        }
        return (T)Locate(typeof(T), diParams);
    }

    public T LocateWithNamedParameters<T>(params (string name, object value)[] parameters)
    {
        var diParams = new IDIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = new NamedParameter(parameters[i].name, parameters[i].value);
        }
        return (T)Locate(typeof(T), diParams);
    }

    public T LocateWithTypedParams<T>(params object[] parameters)
    {
        var diParams = new IDIParameter[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            diParams[i] = new TypedParameter(parameters[i]);
        }
        return (T)Locate(typeof(T), diParams);
    }

    public T Locate<T>(params IDIParameter[] parameters) => (T)Locate(typeof(T), parameters);

    public T Locate<T>(int position, object value) => (T)Locate(typeof(T), new PositionalParameter(position, value));

    public T LocateWithParams<T>(params IDIParameter[] parameters) => (T)Locate(typeof(T), parameters);

    public object Locate(Type type, params IDIParameter[] parameters)
    {
        var isEnumerable = IsEnumerableType(type, out var elementType);
        var resolveType = isEnumerable ? elementType! : type;

        if (_alias.TryGetValue(resolveType, out var alias))
        {
            if (isEnumerable)
            {
                var list = CreateListOfTType(resolveType);

                HashSet<Type> aliasCopy;
                lock (alias)
                {
                    aliasCopy = new HashSet<Type>(alias);
                }

                foreach (var a in aliasCopy)
                {
                    if (LocateInternal(a, isEnumerable, parameters) is IList listOfObj)
                        foreach (var o in listOfObj)
                            list.Add(o);
                }

                return list;
            }
            else
            {
                lock (alias)
                {
                    foreach (var a in alias)
                        return LocateInternal(a, isEnumerable, parameters);
                }

                throw new Exception($"Could not find a concrete type for interface {type.FullName}");
            }
        }
        else
        {
            return LocateInternal(resolveType, isEnumerable, parameters);
        }
    }

    private bool IsEnumerableType(Type type, out Type? elementType)
    {
        if (!type.IsGenericType)
        {
            elementType = null;
            return false;
        }

        // Cache the result for this type
        var isEnumerable = _isEnumerableCache.GetOrAdd(type, t =>
            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        elementType = isEnumerable ? type.GetGenericArguments()[0] : null;
        return isEnumerable;
    }

    private object LocateInternal(Type type, bool isEnumerable, params IDIParameter[] parameters)
    {
        if (_lifetimes.TryGetValue(type, out var lifestyleType) &&
            lifestyleType == LifestyleType.Singleton &&
            _singletonInstances.TryGetValue(type, out var instance))
        {
            if (isEnumerable)
            {
                var singletonList = CreateListOfTType(type);
                singletonList.Add(instance);
                return singletonList;
            }
            return instance;
        }

        if (_constructors.TryGetValue(type, out var constructors))
        {
            List<CachedConstructor[]> constructorsCopy;
            lock (constructors)
            {
                constructorsCopy = new List<CachedConstructor[]>(constructors);
            }

            foreach (var c in constructorsCopy)
            {
                if (isEnumerable)
                {
                    var list = CreateListOfTType(type);

                    foreach (var cc in c)
                    {
                        var paramCount = cc.ParameterValues.Count + parameters.Length;

                        if (cc.Parameters.Length >= paramCount && Locate(cc, type, lifestyleType, parameters, out var val))
                            list.Add(val);
                    }

                    return list;
                }
                else
                {
                    foreach (var cc in c)
                    {
                        var paramCount = cc.ParameterValues.Count + parameters.Length;

                        if (cc.Parameters.Length >= paramCount && Locate(cc, type, lifestyleType, parameters, out var val))
                            return val;
                    }
                }
            }
        }

        return isEnumerable ? CreateListOfTType(type) : Activator.CreateInstance(type)!;
    }

    private IList CreateListOfTType(Type type)
    {
        var factory = _listFactories.GetOrAdd(type, t =>
        {
            var listType = typeof(List<>).MakeGenericType(t);
            var ctor = listType.GetConstructor(Type.EmptyTypes)!;
            var lambda = Expression.Lambda<Func<IList>>(Expression.New(ctor));
            return lambda.Compile();
        });

        return factory();
    }

    private bool Locate(CachedConstructor c, Type type, LifestyleType lifestyleType, IDIParameter[] runtimeParameters, out object val)
    {
        if (c.Parameters.Length == 0)
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

        var parameters = new object?[c.Parameters.Length];
        var found = true;

        // Track which parameters have been consumed using a bitfield for small counts, or HashSet for larger
        // For most DI scenarios, parameter counts are small, so we use a simple bool array
        var registeredParams = c.ParameterValues;
        var consumedRegistered = registeredParams.Count > 0 ? new bool[registeredParams.Count] : Array.Empty<bool>();
        var consumedRuntime = runtimeParameters.Length > 0 ? new bool[runtimeParameters.Length] : Array.Empty<bool>();

        for (var i = 0; i < c.Parameters.Length; i++)
        {
            var p = c.Parameters[i];
            var foundParameter = false;

            // Check registered parameters first
            for (var j = 0; j < registeredParams.Count; j++)
            {
                if (consumedRegistered[j])
                    continue;

                var parameterValue = registeredParams[j];
                if (parameterValue.GetParameterValue(p.Name ?? "", p.Position, p.ParameterType))
                {
                    parameters[i] = parameterValue.Value;
                    foundParameter = true;
                    consumedRegistered[j] = true;
                    break;
                }
            }

            if (!foundParameter)
            {
                // Check runtime parameters
                for (var j = 0; j < runtimeParameters.Length; j++)
                {
                    if (consumedRuntime[j])
                        continue;

                    var parameterValue = runtimeParameters[j];
                    if (parameterValue.GetParameterValue(p.Name ?? "", p.Position, p.ParameterType))
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
            if (_constructors.ContainsKey(p.ParameterType) || _alias.ContainsKey(p.ParameterType))
            {
                parameters[i] = Locate(p.ParameterType);
                continue;
            }

            // Check for optional parameter
            if (p.IsOptional)
            {
                parameters[i] = p.DefaultValue;
                continue;
            }

            found = false;
            break;
        }

        if (!found)
        {
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
}
