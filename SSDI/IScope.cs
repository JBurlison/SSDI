using SSDI.Parameters;

namespace SSDI;

/// <summary>
/// Represents a scope for resolving scoped dependencies.
/// Scoped services are created once per scope and disposed when the scope is disposed.
/// </summary>
/// <remarks>
/// <para>
/// Create a scope using <see cref="Builder.ActivationBuilder.CreateScope"/>.
/// Scoped services must be resolved from a scope, not directly from the container.
/// </para>
/// <para>
/// Common use cases include:
/// <list type="bullet">
///   <item><description>Per-player scope in games (inventory, stats, session data)</description></item>
///   <item><description>Per-request scope in servers (request context, handlers)</description></item>
///   <item><description>Per-scene scope (scene-specific managers and state)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register scoped services
/// container.Configure(c =>
/// {
///     c.Export&lt;PlayerInventory&gt;().As&lt;IInventory&gt;().Lifestyle.Scoped();
///     c.Export&lt;PlayerStats&gt;().Lifestyle.Scoped();
/// });
///
/// // Create a scope (e.g., when player connects)
/// var playerScope = container.CreateScope();
///
/// // Resolve services - same instance within scope
/// var inventory1 = playerScope.Locate&lt;IInventory&gt;();
/// var inventory2 = playerScope.Locate&lt;IInventory&gt;();
/// // inventory1 == inventory2
///
/// // When player disconnects, dispose the scope
/// playerScope.Dispose(); // All scoped IDisposable services are disposed
/// </code>
/// </example>
public interface IScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets whether this scope has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Locates and returns an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    T Locate<T>();

    /// <summary>
    /// Locates and returns an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>An instance of the specified type.</returns>
    object Locate(Type type);

    /// <summary>
    /// Locates and returns an instance with positional constructor parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">The positional parameter values, starting at position 0.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    T LocateWithPositionalParams<T>(params object[] parameters);

    /// <summary>
    /// Locates and returns an instance with named constructor parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Tuples of parameter names and values.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    T LocateWithNamedParameters<T>(params (string name, object value)[] parameters);

    /// <summary>
    /// Locates and returns an instance with typed constructor parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">The parameter values to match by type.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    T LocateWithTypedParams<T>(params object[] parameters);

    /// <summary>
    /// Locates and returns an instance with struct-based DI parameters.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="parameters">Struct-based parameters for optimal performance.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    T Locate<T>(params DIParameter[] parameters);

    /// <summary>
    /// Locates and returns an instance with a single positional parameter.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    /// <param name="position">The zero-based position of the parameter.</param>
    /// <param name="value">The value for the parameter.</param>
    /// <returns>An instance of <typeparamref name="T"/>.</returns>
    T Locate<T>(int position, object value);
}
