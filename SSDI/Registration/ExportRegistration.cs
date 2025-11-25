namespace SSDI.Registration;

/// <summary>
/// Provides methods for registering types and instances with the dependency injection container.
/// </summary>
/// <remarks>
/// This class is passed to the <see cref="DependencyInjectionContainer.Configure"/> method
/// to allow fluent registration of dependencies.
/// </remarks>
public class ExportRegistration
{
    internal List<InternalRegistration> Registrations { get; } = new List<InternalRegistration>();

    /// <summary>
    /// Exports a type for dependency injection with the default transient lifestyle.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    /// <returns>A <see cref="FluentExportRegistration"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;MyService&gt;();                           // Transient (default)
    ///     c.Export&lt;MyRepository&gt;().Lifestyle.Singleton();  // Singleton
    ///     c.Export&lt;PacketRouter&gt;().As&lt;IPacketRouter&gt;();    // With interface alias
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration Export<T>() => Export(typeof(T));

    /// <summary>
    /// Exports a type for dependency injection with the default transient lifestyle.
    /// </summary>
    /// <param name="t">The type to register.</param>
    /// <returns>A <see cref="FluentExportRegistration"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// container.Configure(c =>
    /// {
    ///     c.Export(typeof(MyService));
    ///     c.Export(pluginType).As(pluginInterface);
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration Export(Type t)
    {
        var registration = new InternalRegistration(t);
        Registrations.Add(registration);
        return registration.FluentExportRegistration;
    }

    /// <summary>
    /// Exports an existing instance as a singleton. The lifestyle is automatically set to Singleton.
    /// </summary>
    /// <typeparam name="T">The type of the instance.</typeparam>
    /// <param name="instance">The instance to register.</param>
    /// <returns>A <see cref="FluentExportRegistration"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// var configuration = new ConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .Build();
    /// 
    /// container.Configure(c =>
    /// {
    ///     c.ExportInstance(configuration).As&lt;IConfiguration&gt;();
    ///     c.ExportInstance(new LoggerFactory()).As&lt;ILoggerFactory&gt;();
    /// });
    /// </code>
    /// </example>
    public FluentExportRegistration ExportInstance<T>(T instance)
    {
        var registration = new InternalRegistration(typeof(T), instance);
        Registrations.Add(registration);
        _ = registration.FluentExportRegistration.Lifestyle.Singleton();
        return registration.FluentExportRegistration;
    }
}
