using Hosihikari.Binding;
using Hosihikari.Binding.LeviLamina;
using Microsoft.Extensions.Logging;

namespace Hosihikari.Carrier.LeviLamina;

/// <summary>Installs the server-thread synchronization context for the LeviLamina carrier.</summary>
/// <remarks>
/// The framework calls <see cref="StartAsync"/> on the server thread before event dispatch
/// opens; the context only affects the thread that installs it, so that ordering is required.
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

        _context = ServerSynchronizationContext.Install(
            new ServerExecutor(),
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
