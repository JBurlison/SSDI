using System.Linq.Expressions;
using System.Reflection;
using SSDI.Parameters;

namespace SSDI.Builder;

internal class CachedConstructor
{
    internal ParameterInfo[] Parameters { get; }
    internal List<IDIParameter> ParameterValues { get; }
    internal Func<object?[], object> ConstructorFunc { get; }

    internal CachedConstructor(ConstructorInfo constructor, List<IDIParameter> parameterValues)
    {
        Parameters = constructor.GetParameters();
        ParameterValues = parameterValues;

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
}
