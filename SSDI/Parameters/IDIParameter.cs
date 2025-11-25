namespace SSDI.Parameters
{
    /// <summary>
    /// Defines a contract for dependency injection parameters used to provide values to constructors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SSDI provides three built-in implementations:
    /// <list type="bullet">
    ///   <item><description><see cref="TypedParameter"/> - Matches by parameter type</description></item>
    ///   <item><description><see cref="NamedParameter"/> - Matches by parameter name</description></item>
    ///   <item><description><see cref="PositionalParameter"/> - Matches by parameter position (0-based)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// You can implement this interface to create custom parameter matching logic.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using built-in parameters
    /// var server = container.Locate&lt;TCPServer&gt;(
    ///     new NamedParameter("address", "127.0.0.1"),
    ///     new PositionalParameter(1, 8080)
    /// );
    /// </code>
    /// </example>
    public interface IDIParameter
    {
        /// <summary>
        /// Gets the value to be injected into the constructor parameter.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Determines whether this parameter matches the specified constructor parameter.
        /// </summary>
        /// <param name="parameterName">The name of the constructor parameter.</param>
        /// <param name="parameterPosition">The zero-based position of the constructor parameter.</param>
        /// <param name="parameterType">The type of the constructor parameter.</param>
        /// <returns><c>true</c> if this parameter should be used for the specified constructor parameter; otherwise, <c>false</c>.</returns>
        bool GetParameterValue(string parameterName, int parameterPosition, Type parameterType);
    }
}
