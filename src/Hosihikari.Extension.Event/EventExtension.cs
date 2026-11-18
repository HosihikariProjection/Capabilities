namespace Hosihikari.Extension.Event;

/// <summary>Availability of the event capability in this process.</summary>
/// <remarks>
/// The carrier package constructs its publisher and marks the capability available once its
/// native listeners are attached. That happens after the carrier plugin itself is enabled, and
/// carrier load order is not the plugin's to assume: a plugin's <c>OnEnable</c> can still run
/// before the carrier's, so <see cref="IsAvailable"/> may be false there and become true later.
/// Read it at the point of use; do not cache it in a constructor or in <c>OnLoad</c>, and do not
/// treat a false reading in <c>OnEnable</c> as final.
/// <para>
/// Subscription does not depend on this flag: <c>IPluginContext.Events.Subscribe</c> works
/// regardless, and events start arriving once the publisher attaches. Use
/// <see cref="IsAvailable"/> only for diagnostics or to decide whether to warn the operator.
/// </para>
/// </remarks>
public static class EventExtension
{
    /// <summary>Whether a carrier publisher is attached in this process.</summary>
    public static bool IsAvailable { get; private set; }

    /// <summary>Called by the carrier publisher after its listeners are attached.</summary>
    public static void MarkAvailable() => IsAvailable = true;
}
