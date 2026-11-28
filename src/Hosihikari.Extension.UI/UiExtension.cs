using System.Reflection;
using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.UI;

/// <summary>Entry point for the UI capability.</summary>
/// <remarks>
/// Mirrors the hook capability's selection: the plugin package carries the adapter for its
/// carrier, and the first call probes the known adapter assemblies.
/// </remarks>
public static class UiExtension
{
    private static readonly string[] AdapterAssemblies =
    [
        "Hosihikari.Extension.UI.Endstone",
        "Hosihikari.Extension.UI.LeviLamina",
        "Hosihikari.Extension.UI.Bedrock",
    ];

    private static readonly Lock Gate = new();
    private static IUiService? _service;

    /// <summary>Whether the current carrier can show forms.</summary>
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

    /// <summary>The carrier's UI service.</summary>
    /// <exception cref="CarrierCapabilityUnavailableException">No adapter is present.</exception>
    public static IUiService Service
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

                throw new CarrierCapabilityUnavailableException("UI", "the current carrier");
            }
        }
    }

    /// <summary>Shows an action form to a player.</summary>
    public static void ShowActionForm(
        IPlayerView player,
        ActionFormDefinition form,
        Action<ActionFormResponse>? onSubmit = null,
        Action? onClose = null
    ) => Service.ShowActionForm(player, form, onSubmit, onClose);

    /// <summary>Registers the adapter for the running carrier.</summary>
    /// <remarks>Called by an adapter's module initializer; plugins never call it.</remarks>
    public static void Use(IUiService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        Interlocked.Exchange(ref _service, service);
    }
}
