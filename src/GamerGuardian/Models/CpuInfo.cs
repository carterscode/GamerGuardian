namespace GamerGuardian.Models;

public enum CpuVendor
{
    Unknown,
    Amd,
    Intel,
}

/// <summary>
/// Lightweight detected-CPU shape produced by <c>CpuDetector</c> from the
/// Windows registry processor strings. Carries vendor, the raw name, a
/// normalized model token, and a coarse family tag. CCD topology / parking
/// strategy are NOT decided here — the CPU tune catalog owns that (see
/// <c>CpuTuneCatalog</c>).
/// </summary>
public sealed record CpuInfo(
    CpuVendor Vendor,
    string RawModel,
    string Model,
    string Family)
{
    /// <summary>True when we recognized a vendor and extracted a model token.</summary>
    public bool IsDetected => Vendor != CpuVendor.Unknown && !string.IsNullOrEmpty(Model);

    public static CpuInfo Unknown(string raw = "") =>
        new(CpuVendor.Unknown, raw, string.Empty, string.Empty);
}
