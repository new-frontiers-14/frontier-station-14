using Content.Server._NF.Atmos.Systems;
using Content.Shared._NF.Atmos.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Atmos.Components;

[RegisterComponent, Access(typeof(GasDepositSystem))]
public sealed partial class RandomGasDepositComponent : Component
{
    /// <summary>
    /// The name of the prototype used to populate the gas deposit in this entity.
    /// If null or invalid, will be selected from existing set at random.
    /// </summary>
    [DataField]
    public ProtoId<GasDepositPrototype>? DepositPrototype;
}
