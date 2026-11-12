namespace Hosihikari.Extension.UI;

/// <summary>Which button the player clicked.</summary>
/// <param name="ButtonIndex">The zero-based index into the form's buttons.</param>
public readonly record struct ActionFormResponse(int ButtonIndex);
