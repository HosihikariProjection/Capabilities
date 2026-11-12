using System.Reflection;
using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.Hook;

/// <summary>Entry point for the hook capability.</summary>
/// <remarks>
/// The plugin package carries the adapter for its carrier; the first call probes the known
/// adapter assemblies and keeps the one that registers itself. A plugin that carries no adapter
/// for the running carrier gets <see cref="CarrierCapabilityUnavailableException"/>.
/// </remarks>
public static class HookExtension
{
    private static readonly string[] AdapterAssemblies =
    [
        "Hosihikari.Extension.Hook.LeviLamina",
        "Hosihikari.Extension.Hook.Endstone",
        "Hosihikari.Extension.Hook.Bedrock",
    ];

    private static readonly Lock Gate = new();
    private static IHookService? _service;

    /// <summary>Whether the current carrier can install detours.</summary>
    public static bool IsAvailable
    {
        get
        {
            try
            {
                _ = Service;
                return true;
            }
            catch (CarrierCapabilityUnavailableException)
            {
                return false;
            }
        }
    }

    /// <summary>The carrier's hook service.</summary>
    /// <exception cref="CarrierCapabilityUnavailableException">No adapter is present.</exception>
    public static IHookService Service
    {
        get
        {
            var service = Volatile.Read(ref _service);
            if (service is not null)
            {
                return service;
            }

            lock (Gate)
            {
                foreach (var name in AdapterAssemblies)
                {
                    try
                    {
                        Assembly.Load(name);
                    }
                    catch (Exception)
                    {
                        // A carrier whose adapter is absent simply fails to load; keep probing.
                    }

                    service = Volatile.Read(ref _service);
                    if (service is not null)
                    {
                        return service;
                    }
                }

                throw new CarrierCapabilityUnavailableException("Hook", "the current carrier");
            }
        }
    }

    /// <summary>Installs a detour.</summary>
    public static IHook Attach(
        nint target,
        nint detour,
        HookPriority priority = HookPriority.Normal,
        bool suspendThreads = true) =>
        Service.Attach(target, detour, priority, suspendThreads);

    /// <summary>Registers the adapter for the running carrier.</summary>
    /// <remarks>Called by an adapter's module initializer; plugins never call it.</remarks>
    public static void Use(IHookService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        Interlocked.Exchange(ref _service, service);
    }
}
