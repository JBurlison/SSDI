using System.Runtime.CompilerServices;

namespace SSDI.Parameters;

/// <summary>
/// Specifies how a DI parameter should be matched to constructor parameters.
/// </summary>
public enum DIParameterKind : byte
{
    /// <summary>Match by parameter name.</summary>
    Named,
    /// <summary>Match by parameter position (0-based index).</summary>
    Positional,
    /// <summary>Match by parameter type.</summary>
    Typed
}

/// <summary>
/// A high-performance struct-based parameter for dependency injection.
/// Replaces interface-based parameters to avoid virtual dispatch overhead.
/// </summary>
public readonly struct DIParameter
{
    /// <summary>The matching strategy for this parameter.</summary>
    public readonly DIParameterKind Kind;

    /// <summary>The value to inject.</summary>
    public readonly object Value;

    /// <summary>The parameter name (for Named matching).</summary>
    public readonly string? Name;

    /// <summary>The parameter position (for Positional matching).</summary>
    public readonly int Position;

    /// <summary>The cached type of the value (for Typed matching).</summary>
    public readonly Type? ValueType;

    /// <summary>
    /// Creates a named parameter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DIParameter Named(string name, object value) => new(DIParameterKind.Named, value, name, -1, null);

    /// <summary>
    /// Creates a positional parameter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DIParameter Positional(int position, object value) => new(DIParameterKind.Positional, value, null, position, null);

    /// <summary>
    /// Creates a typed parameter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DIParameter Typed(object value) => new(DIParameterKind.Typed, value, null, -1, value.GetType());

    private DIParameter(DIParameterKind kind, object value, string? name, int position, Type? valueType)
    {
        Kind = kind;
        Value = value;
        Name = name;
        Position = position;
        ValueType = valueType;
    }

    /// <summary>
    /// Determines whether this parameter matches the specified constructor parameter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Matches(string parameterName, int parameterPosition, Type parameterType)
    {
        return Kind switch
        {
            DIParameterKind.Named => parameterName == Name,
            DIParameterKind.Positional => parameterPosition == Position,
            DIParameterKind.Typed => parameterType == ValueType || (ValueType?.IsAssignableTo(parameterType) ?? false),
            _ => false
        };
    }

    /// <summary>
    /// Creates a DIParameter from a legacy IDIParameter interface.
    /// </summary>
    public static DIParameter FromLegacy(IDIParameter legacy)
    {
        return legacy switch
        {
            NamedParameter np => Named(np.Name, np.Value),
            PositionalParameter pp => Positional(pp.Position, pp.Value),
            TypedParameter tp => Typed(tp.Value),
            _ => new DIParameter(DIParameterKind.Typed, legacy.Value, null, -1, legacy.Value.GetType())
        };
    }
}
