namespace Hosihikari.Extension.UI;

/// <summary>A form with a list of buttons.</summary>
/// <param name="Title">The form title.</param>
/// <param name="Content">The body text.</param>
/// <param name="Buttons">The buttons, in display order.</param>
public sealed record ActionFormDefinition(
    string Title,
    string Content,
    IReadOnlyList<ActionButtonDefinition> Buttons);
