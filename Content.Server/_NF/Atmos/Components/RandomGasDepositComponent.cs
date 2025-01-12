using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent]
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
    [ViewVariables]
    public GasMixture Deposit = new();

    /// <summary>
    /// The maximum number of moles for this deposit to be considered "mostly depleted".
    /// </summary>
    [ViewVariables]
    public float LowMoles;
}
