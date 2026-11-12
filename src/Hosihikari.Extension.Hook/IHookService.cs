namespace Hosihikari.Extension.Hook;

/// <summary>Installs function detours through the current carrier.</summary>
/// <remarks>
/// Paper-style runtime hooking is portable as a mechanism; a target is a raw address resolved
/// against a specific carrier build, so an installed hook is tied to that carrier and version.
/// </remarks>
public interface IHookService
{
    /// <summary>Whether the carrier can install detours.</summary>
    bool IsAvailable { get; }

    /// <summary>Installs a detour.</summary>
    /// <param name="target">The address to hook.</param>
    /// <param name="detour">The function to run instead.</param>
    /// <param name="priority">Order among hooks on the same target.</param>
    /// <param name="suspendThreads">Whether to pause threads while patching, when the carrier can.</param>
    IHook Attach(nint target, nint detour, HookPriority priority = HookPriority.Normal, bool suspendThreads = true);
}
