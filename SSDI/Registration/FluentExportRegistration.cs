using SSDI.Parameters;

namespace SSDI.Registration;

/// <summary>
/// Provides a fluent API for configuring type registrations in the dependency injection container.
/// </summary>
/// <remarks>
/// This class allows chaining of configuration methods to set up aliases, constructor parameters, and lifestyles.
/// </remarks>
public class FluentExportRegistration
{
    internal InternalRegistration RegistrationBlock { get; }

    // Lazy initialized - most registrations have 0-1 aliases
    private List<Type>? _alias;
    internal List<Type> Alias => _alias ??= new List<Type>(1);
    internal bool HasAlias => _alias != null && _alias.Count > 0;

    // Store lifestyle directly to avoid extra allocation
    private LifestyleType _lifestyle = LifestyleType.Transient;
    private bool _lifestyleSet;
    internal LifestyleType LifestyleValue => _lifestyle;

    internal void SetLifestyle(LifestyleType lifestyle)
    {
        if (_lifestyleSet)
            throw new InvalidOperationException("Lifestyle already set.");
        _lifestyleSet = true;
        _lifestyle = lifestyle;
    }

    /// <summary>
    /// Gets the lifestyle scope for configuring the registration's lifetime (Singleton or Transient).
    /// </summary>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;MyService&gt;().Lifestyle.Singleton();
    ///     c.Export&lt;MyRepository&gt;().Lifestyle.Transient(); // Default
    /// });
    /// </code>
    /// </example>
    public LifestyleScope Lifestyle => new LifestyleScope(this);

    // Lazy initialized - most registrations have no parameters
    private List<DIParameter>? _parameters;
    internal List<DIParameter> ParametersInternal => _parameters ??= new List<DIParameter>(2);
    internal bool HasParameters => _parameters != null && _parameters.Count > 0;

    internal FluentExportRegistration(InternalRegistration registrationBlock)
    {
        RegistrationBlock = registrationBlock;
    }

    /// <summary>
    /// Registers an alias type (typically an interface) that this type can be resolved as.
    /// Multiple aliases can be registered for a single type.
    /// </summary>
    /// <typeparam name="TAlias">The alias type (usually an interface).</typeparam>
    /// <returns>This registration for method chaining.</returns>
    /// <remarks>
    /// SSDI does not automatically discover implemented interfaces. You must explicitly register
    /// any interfaces you want to resolve by.
    /// </remarks>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     // Register multiple implementations of the same interface
    ///     c.Export&lt;AuthPacketServer&gt;().As&lt;IPacketRouter&gt;();
    ///     c.Export&lt;HomePacketServer&gt;().As&lt;IPacketRouter&gt;();
    ///     c.Export&lt;ShopPacketServer&gt;().As&lt;IPacketRouter&gt;();
    /// });
    /// 
    /// // Later, resolve all implementations
    /// var routes = container.Locate&lt;IEnumerable&lt;IPacketRouter&gt;&gt;();
    /// </code>
    /// </example>
    public FluentExportRegistration As<TAlias>()
    {
        var alias = Alias;
        var t = typeof(TAlias);
        if (!alias.Contains(t)) alias.Add(t);
        return this;
    }

    /// <summary>
    /// Registers an alias type (typically an interface) that this type can be resolved as.
    /// </summary>
    /// <param name="t">The alias type (usually an interface).</param>
    /// <returns>This registration for method chaining.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export(serviceType).As(interfaceType);
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration As(Type t)
    {
        var alias = Alias;
        if (!alias.Contains(t)) alias.Add(t);
        return this;
    }

    /// <summary>
    /// Specifies a constructor parameter value matched by its type.
    /// </summary>
    /// <typeparam name="TParam">The type of the parameter.</typeparam>
    /// <param name="value">The value to use for the parameter.</param>
    /// <returns>This registration for method chaining.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     // For constructor: ClientServer(PacketScope scope, string name)
    ///     c.Export&lt;ClientServer&gt;()
    ///         .WithCtorParam(PacketScope.ClientToAuth)
    ///         .Lifestyle.Singleton();
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration WithCtorParam<TParam>(TParam value) where TParam : notnull
    {
        ParametersInternal.Add(DIParameter.Typed(value));
        return this;
    }

    /// <summary>
    /// Specifies a constructor parameter value matched by its name.
    /// </summary>
    /// <param name="name">The name of the parameter (must match the constructor parameter name exactly).</param>
    /// <param name="value">The value to use for the parameter.</param>
    /// <returns>This registration for method chaining.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     // For constructor: ClientServer(PacketScope scope, string connectionString)
    ///     c.Export&lt;ClientServer&gt;()
    ///         .WithCtorParam("scope", PacketScope.ClientToAuth)
    ///         .WithCtorParam("connectionString", "Server=localhost")
    ///         .Lifestyle.Singleton();
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration WithCtorParam(string name, object value)
    {
        ParametersInternal.Add(DIParameter.Named(name, value));
        return this;
    }

    /// <summary>
    /// Specifies a constructor parameter value matched by its position (0-based index).
    /// </summary>
    /// <param name="position">The zero-based position of the parameter in the constructor.</param>
    /// <param name="value">The value to use for the parameter.</param>
    /// <returns>This registration for method chaining.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     // For constructor: ClientServer(PacketScope scope, string name, int port)
    ///     c.Export&lt;ClientServer&gt;()
    ///         .WithCtorParam(0, PacketScope.ClientToAuth)  // scope at position 0
    ///         .WithCtorParam(2, 8080)                       // port at position 2
    ///         .Lifestyle.Singleton();
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration WithCtorParam(int position, object value)
    {
        ParametersInternal.Add(DIParameter.Positional(position, value));
        return this;
    }

    /// <summary>
    /// Specifies multiple constructor parameters matched by their positions, starting at position 0.
    /// Parameters not provided will be resolved from the container.
    /// </summary>
    /// <param name="parameters">The parameter values in order, starting at position 0.</param>
    /// <returns>This registration for method chaining.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     // For constructor: ClientServer(PacketScope scope, int port, string name)
    ///     c.Export&lt;ClientServer&gt;()
    ///         .WithCtorPositionalParams(PacketScope.ClientToAuth, 1234, "MyConn")
    ///         .Lifestyle.Singleton();
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration WithCtorPositionalParams(params object[] parameters)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            ParametersInternal.Add(DIParameter.Positional(i, parameters[i]));
        }

        return this;
    }

    /// <summary>
    /// Specifies a constructor parameter using a custom <see cref="IDIParameter"/> implementation.
    /// </summary>
    /// <param name="dIParameter">The custom parameter specification.</param>
    /// <returns>This registration for method chaining.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;MyService&gt;()
    ///         .WithCtorParam(new CustomParameter("value"));
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration WithCtorParam(IDIParameter dIParameter)
    {
        ParametersInternal.Add(DIParameter.FromLegacy(dIParameter));
        return this;
    }
}
