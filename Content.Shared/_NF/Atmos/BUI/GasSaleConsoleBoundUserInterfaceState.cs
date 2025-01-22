using Content.Shared.Atmos;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.BUI;

[NetSerializable, Serializable]
public sealed class GasSaleConsoleBoundUserInterfaceState(int appraisal, GasMixture mixture, bool enabled)
    : BoundUserInterfaceState
{
    /// <summary>
    /// Estimated appraisal value of the gas mixture.
    /// </summary>
    public int Appraisal = appraisal;

    /// <summary>
    /// The mixture in the linked sale entity.
    /// </summary>
    public GasMixture Mixture = mixture;

    /// <summary>
    /// Whether or not the buttons on the interface are enabled.
    /// </summary>
    public bool Enabled = enabled;
}

[Serializable, NetSerializable]
public enum GasSaleConsoleUiKey : byte
{
    Key,
}
