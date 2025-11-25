namespace SSDI.Parameters
{
    /// <summary>
    /// A dependency injection parameter that matches constructor parameters by name.
    /// </summary>
    /// <remarks>
    /// The parameter name must match the constructor parameter name exactly (case-sensitive).
    /// </remarks>
    /// <example>
    /// <code>
    /// // For constructor: TCPServer(string address, int port)
    /// var server = container.Locate&lt;TCPServer&gt;(
    ///     new NamedParameter("address", "127.0.0.1"),
    ///     new NamedParameter("port", 8080)
    /// );
    /// 
    /// // Or during registration:
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;TCPServer&gt;()
    ///         .WithCtorParam("address", "127.0.0.1")
    ///         .WithCtorParam("port", 8080);
    /// });
    /// </code>
    /// </example>
    public class NamedParameter : IDIParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedParameter"/> class.
        /// </summary>
        /// <param name="name">The name of the constructor parameter to match.</param>
        /// <param name="value">The value to inject.</param>
        public NamedParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the name of the constructor parameter to match.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value to be injected into the constructor parameter.
        /// </summary>
        public object Value { get; }

        /// <inheritdoc />
        public bool GetParameterValue(string parameterName, int parameterPosition, Type parameterType) => parameterName == Name;
    }
}
