using SSDI.Registration;

namespace SSDI.Events;

/// <summary>
/// Provides data for the <see cref="Builder.ActivationBuilder.Registered"/> event.
/// </summary>
public class RegisteredEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type that was registered with the container.
    /// </summary>
    public Type RegisteredType { get; }

    /// <summary>
    /// Gets the alias types this registration is exposed as, if any.
    /// </summary>
    public IReadOnlyList<Type> Aliases { get; }

    /// <summary>
    /// Gets the lifestyle of the registration (Singleton, Transient, Scoped).
    /// </summary>
    public LifestyleType Lifestyle { get; }

    /// <summary>
    /// Gets a value indicating whether this is a singleton with a pre-existing instance.
    /// </summary>
    public bool HasInstance { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisteredEventArgs"/> class.
    /// </summary>
    /// <param name="registeredType">The type that was registered.</param>
    /// <param name="aliases">The alias types, if any.</param>
    /// <param name="lifestyle">The lifestyle of the registration.</param>
    /// <param name="hasInstance">Whether this is a singleton with a pre-existing instance.</param>
    public RegisteredEventArgs(Type registeredType, IReadOnlyList<Type>? aliases = null, LifestyleType lifestyle = LifestyleType.Transient, bool hasInstance = false)
    {
        RegisteredType = registeredType;
        Aliases = aliases ?? Array.Empty<Type>();
        Lifestyle = lifestyle;
        HasInstance = hasInstance;
    }
}
