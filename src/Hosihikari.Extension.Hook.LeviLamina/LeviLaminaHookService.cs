using System.Runtime.CompilerServices;
using HostHook = Hosihikari.Binding.LeviLamina.Hook;
using HostPriority = Hosihikari.Binding.LeviLamina.HookPriority;

namespace Hosihikari.Extension.Hook.LeviLamina;

/// <summary>The hook capability over the loader's hook library.</summary>
public sealed class LeviLaminaHookService : IHookService
{
    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public IHook Attach(
        nint target,
        nint detour,
        HookPriority priority = HookPriority.Normal,
        bool suspendThreads = true) =>
        new HookToken(HostHook.Attach(target, detour, (HostPriority)priority, suspendThreads));
}

internal sealed class HookToken(HostHook hook) : IHook
{
    public nint Target => hook.Target;

    public nint Detour => hook.Detour;

    public nint Original => hook.Original;

    public bool IsInstalled => hook.IsInstalled;

    public void Dispose() => hook.Dispose();
}

internal static class Registration
{
    [ModuleInitializer]
    internal static void Initialize() => HookExtension.Use(new LeviLaminaHookService());
}
