using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.Event;

/// <summary>A player sent a chat message.</summary>
public sealed class PlayerChatEvent : CancellableExtensionEvent
{
    private string _message;

    /// <summary>Creates the event for the message that was sent.</summary>
    public PlayerChatEvent(IPlayerView player, string message)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>The player that sent the message.</summary>
    public IPlayerView Player { get; }

    /// <summary>
    /// The message. A handler may replace it; the adapter writes the replacement back to the
    /// carrier when the carrier exposes a writable message.
    /// </summary>
    public string Message
    {
        get => _message;
        set => _message = value ?? throw new ArgumentNullException(nameof(value));
    }
}
