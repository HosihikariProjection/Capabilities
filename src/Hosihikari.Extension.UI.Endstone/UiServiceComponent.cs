using Hosihikari;

namespace Hosihikari.Extension.UI.Endstone;

/// <summary>Installs the UI capability on the Endstone carrier.</summary>
/// <remarks>
/// The adapter cannot rely on its module initializer here: the assemblies are loaded by path
/// into a component host that never triggers a type in one, and <c>Assembly.Load</c> of an
/// assembly that is already loaded does not run the initializer either. A carrier component is
/// instantiated and started, so this one does the registration instead — the same shape the
/// event and async adapters use.
/// <para>
/// No enable gate: forms are only shown from a player interaction, which cannot happen before
/// the carrier is enabled.
/// </para>
/// </remarks>
public sealed class UiServiceComponent : ICarrierComponent
{
    private bool _registered;

    /// <inheritdoc/>
    public ValueTask StartAsync(ICarrierComponentContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_registered)
        {
            UiExtension.Use(new EndstoneUiService());
            _registered = true;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken token) => ValueTask.CompletedTask;
}
