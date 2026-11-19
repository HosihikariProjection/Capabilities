using Hosihikari.Binding;
using Hosihikari.Binding.Endstone;
using InteropPlugin = Hosihikari.Binding.Endstone.Interop.endstone.Plugin;
using Microsoft.Extensions.Logging;
using Hosihikari.Carrier;

namespace Hosihikari.Carrier.Endstone;

/// <summary>Installs the server-thread synchronization context for the Endstone carrier.</summary>
/// <remarks>
/// The context only affects the thread that installs it, so the framework calls
/// <see cref="StartAsync"/> on the server thread. Without a host handle the component leaves the
/// context uninstalled instead of failing the boot; the async capability then reports no ambient
/// context.
/// </remarks>
public sealed class ServerSynchronizationContextComponent : ICarrierComponent
{
    private ServerSynchronizationContext? _context;

    /// <inheritdoc/>
    public ValueTask StartAsync(ICarrierComponentContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_context is not null)
        {
            throw new InvalidOperationException("The synchronization context is already installed.");
        }

        if (context.CarrierServices.GetService(typeof(ICarrierHost)) is not ICarrierHost { Plugin: not 0 } host)
        {
            return ValueTask.CompletedTask;
        }

        var scope = BindingScope.For(typeof(ServerSynchronizationContextComponent).Assembly);
        var plugin = Plugin.From(InteropPlugin.Borrow(host.Plugin), scope);
        _context = ServerSynchronizationContext.Install(
            new ServerExecutor(plugin),
            exception => context.Logger.LogError(exception, "A server-thread continuation threw."));
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken token)
    {
        _context?.Uninstall();
        _context = null;
        return ValueTask.CompletedTask;
    }
}
