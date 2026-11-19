using Hosihikari.Binding;
using Hosihikari.Binding.Endstone;
using Hosihikari.Carrier;
using Hosihikari.Carrier.Endstone;
using InteropPlugin = Hosihikari.Binding.Endstone.Interop.endstone.Plugin;
using Microsoft.Extensions.Logging;

namespace Hosihikari.Extension.Async.Endstone;

/// <summary>Installs the async capability on the Endstone carrier.</summary>
/// <remarks>
/// Registration waits for the carrier package to inject the host plugin: the scheduler runs
/// queued work under that plugin's lifetime. Without a host the component leaves the capability
/// unavailable instead of failing the boot, and stopping clears the executor so later calls are
/// loud rather than queued under a disabled plugin.
/// <para>
/// Endstone refuses scheduler submissions until the host plugin is enabled, and the refusal is
/// silent — so the executor is installed through a <see cref="PluginEnableGate"/>; before that
/// the capability reports unavailable rather than accepting work it cannot run.
/// </para>
/// </remarks>
public sealed class AsyncServiceComponent : ICarrierComponent
{
    private readonly EndstoneAsyncService _service = new();
    private PluginEnableGate? _gate;
    private ILogger? _logger;
    private bool _registered;

    /// <inheritdoc/>
    public ValueTask StartAsync(ICarrierComponentContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.CarrierServices.GetService(typeof(ICarrierHost)) is not ICarrierHost { Plugin: not 0 } host)
        {
            return ValueTask.CompletedTask;
        }

        var scope = BindingScope.For(typeof(AsyncServiceComponent).Assembly);
        var plugin = Plugin.From(InteropPlugin.Borrow(host.Plugin), scope);
        _logger = context.Logger;
        _gate = PluginEnableGate.RunWhenEnabled(plugin, () => Install(plugin), _logger);
        return ValueTask.CompletedTask;
    }

    private void Install(Plugin plugin)
    {
        _service.SetExecutor(new ServerExecutor(plugin));
        if (!_registered)
        {
            AsyncExtension.Use(_service);
            _registered = true;
        }
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken token)
    {
        _gate?.Dispose();
        _gate = null;
        _service.SetExecutor(null);
        return ValueTask.CompletedTask;
    }
}
