namespace SSDI.Events;

/// <summary>
/// Provides data for the <see cref="Builder.ActivationBuilder.Unregistered"/> event.
/// </summary>
public class UnregisteredEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type that was unregistered from the container.
    /// </summary>
    public Type UnregisteredType { get; }

    /// <summary>
    /// Gets the singleton instance that was removed, if any.
    /// This will be null if the type was not a singleton or had not been instantiated yet.
    /// </summary>
    public object? Instance { get; }

    /// <summary>
    /// Gets a value indicating whether the instance was disposed during unregistration.
    /// </summary>
    public bool WasDisposed { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnregisteredEventArgs"/> class.
    /// </summary>
    /// <param name="unregisteredType">The type that was unregistered.</param>
    /// <param name="instance">The singleton instance that was removed, if any.</param>
    /// <param name="wasDisposed">Whether the instance was disposed.</param>
    public UnregisteredEventArgs(Type unregisteredType, object? instance = null, bool wasDisposed = false)
    {
        UnregisteredType = unregisteredType;
        Instance = instance;
        WasDisposed = wasDisposed;
    }
}
