using Content.Shared.Atmos;
using Content.Shared.Guidebook;

namespace Content.Server._Starlight.Atmos.Piping.Unary.Components;

[RegisterComponent]
public sealed partial class GasInletSiphonComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    private float _transferRate = 200f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferRate
    {
        get => _transferRate;
        set => _transferRate = Math.Clamp(value, 0f, MaxTransferRate);
    }

    [DataField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    [DataField]
    [GuidebookData]
    public float MaxPressure = Atmospherics.MaxOutputPressure;

    [DataField("outlet")]
    public string OutletName = "pipe";
}
