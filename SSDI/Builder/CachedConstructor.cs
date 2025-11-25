using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using SSDI.Parameters;

namespace SSDI.Builder;

/// <summary>
/// Caches constructor information and provides a compiled factory delegate for fast instantiation.
/// Uses struct-based parameters for optimal performance.
/// </summary>
internal sealed class CachedConstructor
{
    internal ParameterInfo[] Parameters { get; }
    internal DIParameter[] ParameterValues { get; }
    internal Func<object?[], object> ConstructorFunc { get; }
    internal int ParameterCount { get; }

    internal CachedConstructor(ConstructorInfo constructor, List<DIParameter> parameterValues)
    {
        Parameters = constructor.GetParameters();
        ParameterCount = Parameters.Length;
        
        // Store as array for faster iteration (no List overhead)
        ParameterValues = parameterValues.Count > 0 ? parameterValues.ToArray() : Array.Empty<DIParameter>();

        // Compile to Func<object?[], object> for fast invocation (avoids DynamicInvoke)
        var argsParam = Expression.Parameter(typeof(object?[]), "args");

        var convertedArgs = new Expression[Parameters.Length];
        for (var i = 0; i < Parameters.Length; i++)
        {
            var arrayAccess = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            convertedArgs[i] = Expression.Convert(arrayAccess, Parameters[i].ParameterType);
        }

        var newExpr = Expression.New(constructor, convertedArgs);
        var lambda = Expression.Lambda<Func<object?[], object>>(
            Expression.Convert(newExpr, typeof(object)),
            argsParam);

        ConstructorFunc = lambda.Compile();
    }

    /// <summary>
    /// Attempts to match parameters and determine if this constructor can be used.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool CanSatisfy(int runtimeParameterCount)
    {
        return ParameterCount >= ParameterValues.Length + runtimeParameterCount;
    }
}
