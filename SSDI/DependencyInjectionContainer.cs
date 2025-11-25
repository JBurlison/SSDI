using SSDI.Builder;
using SSDI.Registration;

namespace SSDI;

/// <summary>
/// A lightweight dependency injection container for .NET applications.
/// Designed for console apps and games where the container does not need to be "built",
/// allowing runtime extensibility with dynamically loaded DLLs.
/// </summary>
/// <remarks>
/// <para>
/// SSDI (Super Simple Dependency Injection) supports two lifestyles:
/// <list type="bullet">
///   <item><description><see cref="LifestyleType.Transient"/> - Creates a new instance every time (default)</description></item>
///   <item><description><see cref="LifestyleType.Singleton"/> - Creates one instance for the application lifetime</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Basic usage:
/// <code>
/// var container = new DependencyInjectionContainer();
/// 
/// container.Configure(c =>
/// {
///     c.Export&lt;MyService&gt;();
///     c.Export&lt;MyRepository&gt;().Lifestyle.Singleton();
///     c.Export&lt;PacketRouter&gt;().As&lt;IPacketRouter&gt;();
/// });
/// 
/// var service = container.Locate&lt;MyService&gt;();
/// </code>
/// </example>
public class DependencyInjectionContainer : ActivationBuilder
{
    /// <summary>
    /// Configures the container with type registrations.
    /// Can be called multiple times at any point in the application lifecycle.
    /// </summary>
    /// <param name="registration">An action that receives an <see cref="ExportRegistration"/> to configure type exports.</param>
    /// <example>
    /// <code>
    /// var container = new DependencyInjectionContainer();
    /// 
    /// // Initial configuration
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;TCPServer&gt;();
    ///     c.Export&lt;PacketRouter&gt;().Lifestyle.Singleton();
    ///     c.ExportInstance(configuration).As&lt;IConfiguration&gt;();
    /// });
    /// 
    /// // Later, add more registrations (e.g., from a plugin DLL)
    /// container.Configure(c =>
    /// {
    ///     c.Export&lt;PluginService&gt;().As&lt;IPluginService&gt;();
    /// });
    /// </code>
    /// </example>
    public void Configure(Action<ExportRegistration> registration)
    {
        var exportRegistration = new ExportRegistration();
        registration(exportRegistration);
        Add(exportRegistration);
    }
}