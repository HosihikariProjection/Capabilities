using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.Event;

/// <summary>A player left the server.</summary>
/// <remarks>
/// Normalises carriers whose native event names differ: LeviLamina reports a disconnect,
/// Endstone a quit.
/// </remarks>
public sealed class PlayerLeaveEvent : ExtensionEvent
{
    /// <summary>Creates the event for the player that left.</summary>
    public PlayerLeaveEvent(IPlayerView player) =>
        Player = player ?? throw new ArgumentNullException(nameof(player));

    /// <summary>The player that left.</summary>
    public IPlayerView Player { get; }
}
