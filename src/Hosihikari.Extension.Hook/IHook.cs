namespace Hosihikari.Extension.Hook;

/// <summary>A function detour installed by the carrier.</summary>
/// <remarks>
/// Disposing removes the detour. The original trampoline stays valid only while the hook is
/// installed.
/// </remarks>
public interface IHook : IDisposable
{
    /// <summary>The address that was hooked.</summary>
    nint Target { get; }

    /// <summary>The detour that was installed.</summary>
    nint Detour { get; }

    /// <summary>The trampoline that calls the original implementation.</summary>
    nint Original { get; }

    /// <summary>Whether the detour is still installed.</summary>
    bool IsInstalled { get; }
}
