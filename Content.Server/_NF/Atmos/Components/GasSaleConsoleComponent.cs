using Content.Shared._NF.Atmos.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent, Access(typeof(SharedGasDepositSystem))]
public sealed partial class GasSaleConsoleComponent : Component
{
    /// <summary>
    /// Currency type to spawn when gas is sold.
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> CashType = "Credit";

    /// <summary>
    /// The radius around the console in meters to check for gas sale points.
    /// Can be modified individually when mapping, so that consoles have a further reach.
    /// </summary>
    [DataField]
    public int SellPointDistance = 8;

    /// <summary>
    /// The sound to use when gas is sold.
    /// </summary>
    [DataField]
    public SoundSpecifier ApproveSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}
