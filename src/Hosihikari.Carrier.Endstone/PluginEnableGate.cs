using Hosihikari;
using Hosihikari.Binding.Endstone;
using Microsoft.Extensions.Logging;

namespace Hosihikari.Carrier.Endstone;

/// <summary>Runs a callback once the host plugin is enabled, on the server thread.</summary>
/// <remarks>
/// Carrier components start during the carrier's <c>onLoad</c>, but Endstone rejects both
/// listener registration and scheduler submissions until the plugin is enabled — neither call
/// fails loudly, so a component that touched them early would end up "started" with nothing
/// attached.
/// <para>
/// The enable is normally observed through <see cref="CarrierEnableSignal"/>, which the carrier
/// raises from its own <c>onEnable</c> on the server thread; <see cref="RunNow"/> then runs the
/// callback right there, during enable. The poll timer is only a fallback (and the timeout), for
/// the case where the signal never arrives.
/// </para>
/// </remarks>
public sealed class PluginEnableGate : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    private readonly Plugin _plugin;
    private readonly Action _onEnabled;
    private readonly ILogger? _logger;
    private readonly TimeSpan _timeout;
    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private Timer? _timer;
    private int _claimed;
    private bool _disposed;

    private PluginEnableGate(Plugin plugin, Action onEnabled, ILogger? logger, TimeSpan timeout)
    {
        _plugin = plugin;
        _onEnabled = onEnabled;
        _logger = logger;
        _timeout = timeout;
    }

    /// <summary>Invokes <paramref name="onEnabled"/> now or once the plugin becomes enabled.</summary>
    /// <returns>A token that cancels a still-pending wait; already-run gates return null.</returns>
    public static PluginEnableGate? RunWhenEnabled(
        Plugin plugin,
        Action onEnabled,
        ILogger? logger = null,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(onEnabled);

        if (plugin.Interop.isEnabled())
        {
            onEnabled();
            return null;
        }

        // Generous on purpose: the wait spans the carrier's whole startup. A server with a large
        // world loads for well over a minute before plugins are enabled, and giving up early
        // leaves the capability dead for the rest of the session — far worse than a late start.
        var gate = new PluginEnableGate(plugin, onEnabled, logger, timeout ?? TimeSpan.FromMinutes(5));

        gate._timer = new Timer(static state => ((PluginEnableGate)state!).Poll(), gate,
            PollInterval, PollInterval);
        CarrierEnableSignal.Subscribe(gate.OnCarrierEnabled);

        // The signal may have been raised between the isEnabled check above and this
        // subscription, which would leave the gate waiting for an event already past. Take the
        // same path the signal takes, not Poll: this lands inside the enable, where the thread
        // check in Poll is known to answer wrong.
        if (CarrierEnableSignal.HasRaised)
        {
            gate.RunNow();
        }

        return gate;
    }

    /// <summary>The carrier announced its own enable; settle immediately.</summary>
    /// <remarks>
    /// Runs on whatever thread raised the signal, which is the carrier's <c>onEnable</c> — the
    /// one instant the enabled flag is already set. The thread check is deliberately skipped
    /// here: during <c>EndstoneServer::init</c> the server thread id is not recorded yet, so
    /// <c>isPrimaryThread()</c> answers false and the callback would be queued onto a tick that
    /// runs only after every child <c>OnEnable</c> has already returned.
    /// </remarks>
    private void OnCarrierEnabled() => RunNow();

    /// <summary>Settles the gate on the calling thread, without a thread check.</summary>
    /// <remarks>
    /// Only correct when the caller already knows it is on the server thread — the signal, and
    /// the late-subscription backstop in <see cref="RunWhenEnabled"/>. The check is skipped
    /// because <c>isPrimaryThread()</c> answers false during <c>EndstoneServer::init</c>: the
    /// server thread id is not recorded yet, so it falls through to comparing <c>mainThread()</c>
    /// (server.cpp:693-705), and the callback would be queued onto a tick that runs only after
    /// every child <c>OnEnable</c> has already returned.
    /// </remarks>
    private void RunNow()
    {
        if (Interlocked.Exchange(ref _claimed, 1) != 0)
        {
            return;
        }

        Interlocked.Exchange(ref _timer, null)?.Dispose();

        if (_disposed)
        {
            return;
        }

        try
        {
            _onEnabled();
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "The deferred start threw on the carrier enable.");
        }
    }

    /// <summary>The poll path: the timeout, and a timer that beat the signal.</summary>
    /// <remarks>
    /// Reached from a timer thread. Its claim has to be released again by running synchronously
    /// whenever the signal has already been raised — see the branch below for why handing that
    /// case to the scheduler loses the race.
    /// </remarks>
    private void Poll()
    {
        if (_disposed)
        {
            return;
        }

        if (!_plugin.Interop.isEnabled())
        {
            if (DateTime.UtcNow - _startedUtc > _timeout)
            {
                Dispose();
                _logger?.LogError(
                    "The host plugin was not enabled within {Seconds}s; the deferred start is abandoned.",
                    _timeout.TotalSeconds);
            }

            return;
        }

        if (Interlocked.Exchange(ref _claimed, 1) != 0)
        {
            return;
        }

        var timer = Interlocked.Exchange(ref _timer, null);
        timer?.Dispose();

        if (_disposed)
        {
            return;
        }

        // Run it here when this really is the server thread, and also whenever the signal has
        // already been raised: in that case the carrier is genuinely enabled, and the timer can
        // otherwise steal the claim from the signal just before it fires — it would then hand the
        // work to a tick that runs after every child OnEnable has already returned. The thread
        // check cannot be relied on to tell those apart: during EndstoneServer::init it answers
        // false even on the server thread (server.cpp:693-705).
        if (IsServerThread() || CarrierEnableSignal.HasRaised)
        {
            try
            {
                if (!_disposed)
                {
                    _onEnabled();
                }
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, "The deferred start threw on the server thread.");
            }

            return;
        }

        try
        {
            // The carrier accepts scheduler work now that the plugin is enabled; the callback
            // must still run on the server thread because it touches carrier state.
            new ServerExecutor(_plugin).Post(() =>
            {
                if (!_disposed)
                {
                    _onEnabled();
                }
            });
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "The deferred start could not be scheduled on the server thread.");
        }
    }

    private bool IsServerThread()
    {
        try
        {
            return _plugin.Interop.getServer()?.isPrimaryThread() ?? false;
        }
        catch
        {
            // A probe failure must not lose the start; falling back to the scheduler is correct
            // on every thread, only slower.
            return false;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;

        CarrierEnableSignal.Unsubscribe(OnCarrierEnabled);
        Interlocked.Exchange(ref _timer, null)?.Dispose();
    }
}
