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
    Singleton
}

/// <summary>
/// Provides methods for configuring the lifetime of a registered type.
/// </summary>
/// <remarks>
/// Access this through <see cref="FluentExportRegistration.Lifestyle"/>.
/// </remarks>
public class LifestyleScope
{
    private readonly FluentExportRegistration _registrationBlock;
    private bool _set = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="LifestyleScope"/> class.
    /// </summary>
    /// <param name="registrationBlock">The parent registration.</param>
    public LifestyleScope(FluentExportRegistration registrationBlock) => _registrationBlock = registrationBlock;

    /// <summary>
    /// Gets or sets the current lifestyle type. Default is <see cref="LifestyleType.Transient"/>.
    /// </summary>
    public LifestyleType Lifestyle { get; set; } = LifestyleType.Transient;

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
        if (_set)
            throw new InvalidOperationException("Lifestyle already set.");

        _set = true;
        Lifestyle = LifestyleType.Singleton;
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
        if (_set)
            throw new InvalidOperationException("Lifestyle already set.");

        _set = true;
        Lifestyle = LifestyleType.Transient;
        return _registrationBlock;
    }

    internal void Set() => _set = true;
}
