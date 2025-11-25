namespace SSDI.Parameters;

/// <summary>
/// A dependency injection parameter that matches constructor parameters by their type.
/// </summary>
/// <remarks>
/// <para>
/// The parameter will match if the constructor parameter type equals or is assignable from
/// the value's type. This supports inheritance and interface implementations.
/// </para>
/// <para>
/// Be careful when using typed parameters with common types like <see cref="string"/> or <see cref="int"/>
/// if the constructor has multiple parameters of the same type. In such cases, consider using
/// <see cref="NamedParameter"/> or <see cref="PositionalParameter"/> instead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // For constructor: TCPServer(string address, int port)
/// var server = container.LocateWithTypedParams&lt;TCPServer&gt;("127.0.0.1", 8080);
/// 
/// // Or with explicit TypedParameter:
/// var server = container.Locate&lt;TCPServer&gt;(
///     new TypedParameter("127.0.0.1"),
///     new TypedParameter(8080)
/// );
/// 
/// // Or during registration:
/// container.Configure(c =>
/// {
///     c.Export&lt;ClientServer&gt;()
///         .WithCtorParam(PacketScope.ClientToAuth);  // Matched by type
/// });
/// </code>
/// </example>
public class TypedParameter : IDIParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedParameter"/> class.
    /// </summary>
    /// <param name="value">The value to inject. The value's type will be used for matching.</param>
    public TypedParameter(object value)
    {
        Value = value;
        ValueType = value.GetType();
    }

    /// <summary>
    /// Gets the value to be injected into the constructor parameter.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Gets the type of the value, used for matching constructor parameters.
    /// </summary>
    public Type ValueType { get; }

    /// <inheritdoc />
    public bool GetParameterValue(string parameterName, int parameterPosition, Type parameterType) => parameterType == ValueType || ValueType.IsAssignableTo(parameterType);
}
