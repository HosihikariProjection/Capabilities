using System.IO;
using Hosihikari.Carrier;
using Microsoft.Extensions.DependencyInjection;
using InteropNativeMethods = Hosihikari.Binding.LeviLamina.Interop.NativeMethods;

namespace Hosihikari.Carrier.LeviLamina.Bootstrap;

/// <summary>Registers the LeviLamina carrier host before any component starts.</summary>
/// <remarks>
/// The real work is loading the binding shim from the carrier package so the binding types
/// resolve. The host handle is the carrier mod's own module handle, which the native side
/// passes down: LeviLamina resolves a listener's owning mod from it, and a director created
/// without it carries no mod at all, so the native side would refuse every listener.
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
                path
            );
        }

        InteropNativeMethods.Load(path);
        context.Services.AddSingleton(typeof(ICarrierHost), new LeviLaminaCarrierHost(context.HostHandle));
    }

    private const string ShimFile = "Hosihikari.Binding.LeviLamina.Shim.dll";
}

/// <summary>The host handle for LeviLamina: the carrier mod's module handle.</summary>
public sealed class LeviLaminaCarrierHost(nint modHandle) : ICarrierHost
{
    /// <inheritdoc/>
    public nint Plugin => modHandle;
}
