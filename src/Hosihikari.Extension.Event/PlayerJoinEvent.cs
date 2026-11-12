using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.Event;

/// <summary>A player joined the server.</summary>
public sealed class PlayerJoinEvent : ExtensionEvent
{
    /// <summary>Creates the event for the player that joined.</summary>
    public PlayerJoinEvent(IPlayerView player) =>
        Player = player ?? throw new ArgumentNullException(nameof(player));

    /// <summary>The player that joined.</summary>
    public IPlayerView Player { get; }
}
