using Content.Shared._NF.Atmos.Prototypes;
using Content.Shared._NF.Atmos.Systems;
using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Atmos.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGasDepositSystem))]
public sealed partial class RandomGasDepositComponent : Component
{
    /// <summary>
    /// The name of the node that is available to dock.
    /// If null or invalid, will be selected from existing set at random.
    /// </summary>
    [DataField]
    public ProtoId<GasDepositPrototype>? DepositPrototype;

    /// <summary>
    /// Gases left in the deposit.
    /// </summary>
    [DataField]
    public GasMixture Deposit = new();

    /// <summary>
    /// The maximum number of moles for this deposit to be considered "mostly depleted".
    /// </summary>
    [ViewVariables]
    public float LowMoles;
}
