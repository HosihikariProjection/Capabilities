namespace Hosihikari.Carrier.Endstone;

/// <summary>The native handles the Endstone carrier package injects into the component context.</summary>
/// <remarks>
/// The carrier package registers an implementation in <c>ICarrierComponentContext.CarrierServices</c>
/// under this type. The handle is an <c>endstone::Plugin*</c>; the framework core never sees it.
/// </remarks>
public interface ICarrierHost
{
    /// <summary>The host plugin handle, or zero when the carrier package has none yet.</summary>
    nint Plugin { get; }
}
