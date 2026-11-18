using System.IO;
using Microsoft.Extensions.DependencyInjection;
using InteropNativeMethods = Hosihikari.Bindings.Endstone.Interop.NativeMethods;
using Hosihikari.Carrier;

namespace Hosihikari.Carrier.Endstone;

/// <summary>Registers the Endstone carrier host before any component starts.</summary>
/// <remarks>
/// Two jobs, both pure data: load the binding shims from the carrier package so the binding
/// types resolve at all, and publish the host handle the components borrow. It runs before the
/// container exists and before any component, so it must not touch a server or a level — the
/// components construct their schedulers from the handle themselves.
/// </remarks>
public sealed class EndstoneCarrierServicesConfigurator : ICarrierServicesConfigurator
{
    /// <inheritdoc/>
    public void Configure(ICarrierServiceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // A missing shim fails the whole boot here, where the reason is still legible; the
        // components would otherwise throw far from the cause on their first binding call.
        var name = ShimFile;
        var path = Path.Combine(context.CarrierDirectory, name);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"The carrier package at {context.CarrierDirectory} does not contain {name}.",
                path);
        }

        Load(path);

        context.Services.AddSingleton(typeof(ICarrierHost), new EndstoneCarrierHost(context.HostHandle));
    }

    /// <summary>The shim as the runtime packages it, per platform.</summary>
    /// <remarks>
    /// The name carries the platform's own library convention: the Windows shim is a DLL and the
    /// Linux one is a version-less .so. Renaming either to a single spelling would make every
    /// build and packaging rule disagree with what the loader actually produced.
    /// </remarks>
    private static string ShimFile => OperatingSystem.IsWindows()
        ? "Hosihikari.Bindings.Endstone.Shim.dll"
        : "libHosihikari.Bindings.Endstone.Shim.so";

    /// <summary>Loads one shim; the binding library keeps its own handle.</summary>
    private static void Load(string path) => InteropNativeMethods.Load(path);
}

/// <summary>The host handle the carrier package hands to its components.</summary>
/// <param name="plugin">The host <c>endstone::Plugin*</c>, or zero when the loader had none.</param>
public sealed class EndstoneCarrierHost(nint plugin) : ICarrierHost
{
    /// <inheritdoc/>
    public nint Plugin => plugin;
}
