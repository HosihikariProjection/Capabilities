namespace Hosihikari.Extension.Event;

/// <summary>Availability of the event capability in this process.</summary>
/// <remarks>
/// The carrier package constructs its publisher and marks the capability available once its
/// native listeners are attached. Plugins should read this after <c>OnEnable</c>, not cache it
/// earlier.
/// </remarks>
public static class EventExtension
{
    /// <summary>Whether a carrier publisher is attached in this process.</summary>
    public static bool IsAvailable { get; private set; }

    /// <summary>Called by the carrier publisher after its listeners are attached.</summary>
    public static void MarkAvailable() => IsAvailable = true;
}
