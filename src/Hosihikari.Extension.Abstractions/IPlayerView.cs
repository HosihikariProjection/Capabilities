namespace Hosihikari.Extension.Abstractions;

/// <summary>A carrier-neutral view of a player.</summary>
/// <remarks>
/// The view is valid only for the callback that handed it out; it is not a handle to keep.
/// Carriers that cannot supply a field return an empty string rather than hiding it.
/// </remarks>
public interface IPlayerView
{
    /// <summary>The player's name as the server reports it.</summary>
    string Name { get; }

    /// <summary>The player's Xbox user id, or an empty string when the carrier has none.</summary>
    string Xuid { get; }

    /// <summary>
    /// The carrier's own player object (its Idiomatic type). Reading it makes the caller
    /// carrier-specific; <see langword="null"/> when the adapter did not supply one.
    /// </summary>
    object? NativeView { get; }
}
