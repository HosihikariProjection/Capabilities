using System.Runtime.CompilerServices;
using Hosihikari.Binding;
using Hosihikari.Binding.Endstone;
using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.UI.Endstone;

/// <summary>The UI capability over Endstone's form classes.</summary>
public sealed class EndstoneUiService : IUiService
{
    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public void ShowActionForm(
        IPlayerView player,
        ActionFormDefinition form,
        Action<ActionFormResponse>? onSubmit = null,
        Action? onClose = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(form);

        if (player.NativeView is not Player native)
        {
            throw new ArgumentException(
                "The player view was not created by the Endstone adapter.", nameof(player));
        }

        var scope = BindingScope.For(typeof(EndstoneUiService).Assembly);
        using var nativeForm = new OwnedActionForm(scope, form.Title, form.Content);
        for (var index = 0; index < form.Buttons.Count; index++)
        {
            var clicked = index;
            var button = form.Buttons[index];
            nativeForm.AddButton(
                button.Text,
                button.Icon,
                _ => onSubmit?.Invoke(new ActionFormResponse(clicked)));
        }

        if (onClose is not null)
        {
            nativeForm.SetOnClose(_ => onClose());
        }

        nativeForm.Show(native);
    }
}

internal static class Registration
{
    [ModuleInitializer]
    internal static void Initialize() => UiExtension.Use(new EndstoneUiService());
}
