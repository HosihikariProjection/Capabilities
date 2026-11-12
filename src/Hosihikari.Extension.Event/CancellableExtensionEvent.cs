namespace Hosihikari.Extension.Event;

/// <summary>An event a handler can stop; the adapter writes the decision back to the carrier.</summary>
public abstract class CancellableExtensionEvent : ExtensionEvent
{
    /// <summary>Whether a handler asked for the carrier action to be cancelled.</summary>
    public bool IsCancelled { get; private set; }

    /// <summary>Asks the carrier to cancel the action this event describes.</summary>
    public void Cancel() => IsCancelled = true;
}
