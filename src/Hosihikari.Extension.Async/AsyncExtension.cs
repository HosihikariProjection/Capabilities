using System.Reflection;
using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.Async;

/// <summary>Entry point for the async capability.</summary>
/// <remarks>
/// Mirrors the other capabilities: the plugin package carries the adapter for its carrier,
/// and the first call probes the known adapter assemblies.
/// </remarks>
public static class AsyncExtension
{
    private static readonly string[] AdapterAssemblies =
    [
        "Hosihikari.Extension.Async.LeviLamina",
        "Hosihikari.Extension.Async.Endstone",
        "Hosihikari.Extension.Async.Bedrock",
    ];

    private static readonly Lock Gate = new();
    private static IAsyncService? _service;

    /// <summary>Whether the current carrier can schedule onto the server thread.</summary>
    /// <remarks>
    /// On carriers whose components install behind an enable gate this can read false during a
    /// plugin's early startup and become true later; read it at the point of use rather than
    /// caching it.
    /// </remarks>
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

    /// <summary>The carrier's async service.</summary>
    /// <exception cref="CarrierCapabilityUnavailableException">No adapter is present.</exception>
    public static IAsyncService Service
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

                throw new CarrierCapabilityUnavailableException("Async", "the current carrier");
            }
        }
    }

    /// <summary>Queues work to run on the server thread; it is not observed.</summary>
    public static void Post(Action work) => Service.Post(work);

    /// <summary>Runs work on the server thread and completes there.</summary>
    public static Task RunAsync(Action work) => Service.RunAsync(work);

    /// <summary>
    /// Switches the caller's continuation to the server thread: <c>await AsyncExtension.SwitchToServerThread();</c>
    /// </summary>
    public static ServerThreadAwaitable SwitchToServerThread() => default;

    /// <summary>Registers the adapter for the running carrier.</summary>
    /// <remarks>Called by an adapter's module initializer; plugins never call it.</remarks>
    public static void Use(IAsyncService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        Interlocked.Exchange(ref _service, service);
    }
}
