using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.UI;

/// <summary>Shows carrier-native forms to players.</summary>
public interface IUiService
{
    /// <summary>Whether the carrier can show forms.</summary>
    bool IsAvailable { get; }

    /// <summary>Shows an action form to a player.</summary>
    /// <param name="player">The target; its <c>NativeView</c> must come from this adapter.</param>
    /// <param name="form">The form to show.</param>
    /// <param name="onSubmit">Called on the carrier's thread with the clicked button.</param>
    /// <param name="onClose">Called when the player closes the form without choosing.</param>
    void ShowActionForm(
        IPlayerView player,
        ActionFormDefinition form,
        Action<ActionFormResponse>? onSubmit = null,
        Action? onClose = null);
}
