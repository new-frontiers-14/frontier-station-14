using Content.Server._NF.Atmos.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent, Access(typeof(GasDepositSystem))]
public sealed partial class GasSaleConsoleComponent : Component
{
    // Currency type to spawn when gas is sold.
    [DataField]
    public ProtoId<StackPrototype> CashType = "Credit";

    // The radius around the console in meters to check for gas sale points.
    // Can be modified individually when mapping, so that consoles have a further reach.
    [DataField]
    public int SellPointDistance = 8;
}
