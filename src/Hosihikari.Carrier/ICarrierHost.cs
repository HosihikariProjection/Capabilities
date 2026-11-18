namespace Hosihikari.Carrier;

/// <summary>The native host handle a carrier package injects into the component context.</summary>
/// <remarks>
/// The carrier package registers an implementation in <c>ICarrierComponentContext.CarrierServices</c>
/// under this type; every carrier uses the same key so one bootstrap shape serves all of them.
/// The handle is carrier-specific (an <c>endstone::Plugin*</c> on Endstone, zero on LeviLamina);
/// the framework core never sees it. Resolution is by exact <see cref="Type"/>, so this type must
/// live in one assembly shared by every carrier package and component.
/// </remarks>
public interface ICarrierHost
{
    /// <summary>The host plugin handle, or zero when the carrier has none.</summary>
    nint Plugin { get; }
}
