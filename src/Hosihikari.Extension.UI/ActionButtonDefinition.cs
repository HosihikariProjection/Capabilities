namespace Hosihikari.Extension.UI;

/// <summary>A button on an action form.</summary>
/// <param name="Text">The label the player sees.</param>
/// <param name="Icon">An optional icon path or texture name.</param>
public sealed record ActionButtonDefinition(string Text, string? Icon = null);
