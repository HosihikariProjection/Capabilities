using Hosihikari.Binding.Endstone;
using Microsoft.Extensions.Logging;

namespace Hosihikari.Carrier.Endstone;

/// <summary>Runs a callback once the host plugin is enabled, on the server thread.</summary>
/// <remarks>
/// Carrier components start during the carrier's <c>onLoad</c>, but Endstone rejects both
/// listener registration and scheduler submissions until the plugin is enabled — neither call
/// fails loudly, so a component that touched them early would end up "started" with nothing
/// attached. This gate polls the enable flag and then hops to the server thread through the
/// scheduler, which is accepted from that point on.
/// <para>
/// Only the enable flag is read from the polling thread; every call that touches carrier state
/// happens inside the callback, on the server thread.
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

        var gate = new PluginEnableGate(plugin, onEnabled, logger, timeout ?? TimeSpan.FromSeconds(30));
        gate._timer = new Timer(static state => ((PluginEnableGate)state!).Poll(), gate,
            PollInterval, PollInterval);
        return gate;
    }

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

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
        Interlocked.Exchange(ref _timer, null)?.Dispose();
    }
}
