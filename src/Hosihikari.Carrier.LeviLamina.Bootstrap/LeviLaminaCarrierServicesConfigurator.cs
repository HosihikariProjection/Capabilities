using System.IO;
using Microsoft.Extensions.DependencyInjection;
using InteropNativeMethods = Hosihikari.Bindings.LeviLamina.Interop.NativeMethods;
using Hosihikari.Carrier;

namespace Hosihikari.Carrier.LeviLamina;

/// <summary>Registers the LeviLamina carrier host before any component starts.</summary>
/// <remarks>
/// LeviLamina has no host plugin object to hand over, so the registered handle is zero and the
/// components never look at it. The real work is loading the binding shim from the carrier
/// package so the binding types resolve.
/// </remarks>
public sealed class LeviLaminaCarrierServicesConfigurator : ICarrierServicesConfigurator
{
    /// <inheritdoc/>
    public void Configure(ICarrierServiceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = Path.Combine(context.CarrierDirectory, ShimFile);
        if (!File.Exists(path))
        {
            // Fail the boot here: a component would otherwise throw on its first binding call,
            // far from the actual cause.
            throw new FileNotFoundException(
                $"The carrier package at {context.CarrierDirectory} does not contain {ShimFile}.",
                path);
        }

        InteropNativeMethods.Load(path);
        context.Services.AddSingleton(typeof(ICarrierHost), new LeviLaminaCarrierHost());
    }

    private const string ShimFile = "Hosihikari.Bindings.LeviLamina.Shim.dll";
}

/// <summary>The host handle for LeviLamina: always zero.</summary>
public sealed class LeviLaminaCarrierHost : ICarrierHost
{
    /// <inheritdoc/>
    public nint Plugin => 0;
}
