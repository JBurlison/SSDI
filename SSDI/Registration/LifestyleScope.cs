namespace SSDI.Registration;

/// <summary>
/// Specifies the lifetime management strategy for registered types.
/// </summary>
public enum LifestyleType
{
    /// <summary>
    /// Every time a dependency is resolved, a new instance is created.
    /// This is the default lifestyle.
    /// </summary>
    Transient,

    /// <summary>
    /// A single instance is created for the lifetime of the application.
    /// The instance is created lazily on first resolution.
    /// </summary>
    Singleton,

    /// <summary>
    /// A single instance is created per scope. Different scopes get different instances.
    /// Scoped instances are disposed when the scope is disposed.
    /// </summary>
    /// <remarks>
    /// Scoped services must be resolved from an <see cref="IScope"/> created via
    /// <see cref="Builder.ActivationBuilder.CreateScope"/>. Attempting to resolve
    /// a scoped service directly from the container will throw an exception.
    /// </remarks>
    Scoped
}

/// <summary>
/// Provides methods for configuring the lifetime of a registered type.
/// </summary>
/// <remarks>
/// Access this through <see cref="FluentExportRegistration.Lifestyle"/>.
/// This is a lightweight struct that delegates to the parent registration.
/// </remarks>
public readonly struct LifestyleScope
{
    private readonly FluentExportRegistration _registrationBlock;

    /// <summary>
    /// Initializes a new instance of the <see cref="LifestyleScope"/> struct.
    /// </summary>
    /// <param name="registrationBlock">The parent registration.</param>
    public LifestyleScope(FluentExportRegistration registrationBlock)
    {
        _registrationBlock = registrationBlock;
    }

    /// <summary>
    /// Gets the current lifestyle type. Default is <see cref="LifestyleType.Transient"/>.
    /// </summary>
    public LifestyleType Lifestyle => _registrationBlock.LifestyleValue;

    /// <summary>
    /// Configures the registration to use singleton lifetime.
    /// A single instance is created for the lifetime of the application.
    /// Can only be called once per registration.
    /// </summary>
    /// <returns>The parent <see cref="FluentExportRegistration"/> for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the lifestyle has already been set.</exception>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;DatabaseConnection&gt;().Lifestyle.Singleton();
    ///     c.Export&lt;CacheService&gt;().Lifestyle.Singleton();
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration Singleton()
    {
        _registrationBlock.SetLifestyle(LifestyleType.Singleton);
        return _registrationBlock;
    }

    /// <summary>
    /// Configures the registration to use transient lifetime (default).
    /// A new instance is created every time the type is resolved.
    /// Can only be called once per registration.
    /// </summary>
    /// <returns>The parent <see cref="FluentExportRegistration"/> for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the lifestyle has already been set.</exception>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;RequestHandler&gt;().Lifestyle.Transient();  // Explicit
    ///     c.Export&lt;CommandProcessor&gt;();                       // Also transient (default)
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration Transient()
    {
        _registrationBlock.SetLifestyle(LifestyleType.Transient);
        return _registrationBlock;
    }

    /// <summary>
    /// Configures the registration to use scoped lifetime.
    /// A single instance is created per scope, and disposed when the scope is disposed.
    /// Can only be called once per registration.
    /// </summary>
    /// <returns>The parent <see cref="FluentExportRegistration"/> for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the lifestyle has already been set.</exception>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;PlayerInventory&gt;().Lifestyle.Scoped();
    ///     c.Export&lt;PlayerStats&gt;().As&lt;IPlayerStats&gt;().Lifestyle.Scoped();
    /// });
    ///
    /// // Create a scope per player
    /// using var playerScope = container.CreateScope();
    /// var inventory = playerScope.Locate&lt;PlayerInventory&gt;(); // Same instance within scope
    /// </code>
    /// </example>
    public FluentExportRegistration Scoped()
    {
        _registrationBlock.SetLifestyle(LifestyleType.Scoped);
        return _registrationBlock;
    }
}
