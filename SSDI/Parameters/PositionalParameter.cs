namespace SSDI.Parameters
{
    /// <summary>
    /// A dependency injection parameter that matches constructor parameters by their position (0-based index).
    /// </summary>
    /// <remarks>
    /// Position is zero-based, so the first parameter is position 0, second is position 1, etc.
    /// </remarks>
    /// <example>
    /// <code>
    /// // For constructor: TCPServer(string address, int port, string name)
    /// var server = container.Locate&lt;TCPServer&gt;(
    ///     new PositionalParameter(0, "127.0.0.1"),  // address
    ///     new PositionalParameter(1, 8080),         // port
    ///     new PositionalParameter(2, "MainServer")  // name
    /// );
    /// 
    /// // Or during registration:
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;TCPServer&gt;()
    ///         .WithCtorParam(0, "127.0.0.1")
    ///         .WithCtorParam(1, 8080);
    /// });
    /// </code>
    /// </example>
    public class PositionalParameter : IDIParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositionalParameter"/> class.
        /// </summary>
        /// <param name="position">The zero-based position of the constructor parameter to match.</param>
        /// <param name="value">The value to inject.</param>
        public PositionalParameter(int position, object value)
        {
            Position = position;
            Value = value;
        }

        /// <summary>
        /// Gets the zero-based position of the constructor parameter to match.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets the value to be injected into the constructor parameter.
        /// </summary>
        public object Value { get; }

        /// <inheritdoc />
        public bool GetParameterValue(string parameterName, int parameterPosition, Type parameterType) => parameterPosition == Position;
    }
}