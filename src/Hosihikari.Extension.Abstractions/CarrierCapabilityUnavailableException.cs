namespace Hosihikari.Extension.Abstractions;

/// <summary>The current carrier cannot provide the requested capability.</summary>
public sealed class CarrierCapabilityUnavailableException : NotSupportedException
{
    /// <summary>Creates the exception for a capability and the carrier that lacks it.</summary>
    public CarrierCapabilityUnavailableException(string capability, string carrier)
        : base($"The {capability} capability is not available on the {carrier} carrier.")
    {
        Capability = capability;
        Carrier = carrier;
    }

    /// <summary>The capability that was requested.</summary>
    public string Capability { get; }

    /// <summary>The carrier that does not provide it.</summary>
    public string Carrier { get; }
}
