namespace SSDI.Registration;

internal class InternalRegistration
{
    internal Type ExportedType { get; }
    internal FluentExportRegistration FluentExportRegistration { get; }
    internal object? Instance { get; }

    internal InternalRegistration(Type t, object? instance = null)
    {
        ExportedType = t;
        FluentExportRegistration = new FluentExportRegistration(this);
        Instance = instance;
    }
}
