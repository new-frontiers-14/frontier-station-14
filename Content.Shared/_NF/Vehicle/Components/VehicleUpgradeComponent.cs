using Content.Shared.Construction.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleUpgradeComponent : Component
{
    public const string PartContainerName = "machine_parts";

    /// <summary>
    /// Contains the vehicle's machine parts.
    /// </summary>
    [ViewVariables]
    public Container PartContainer = default!;

    /// <summary>
    /// Machine parts needed to upgrade this vehicle.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<MachinePartPrototype>, int> Requirements = default!;

    /// <summary>
    /// Available upgrades for this vehicle.
    /// Note: Only one machine part per upgradable property. Can't have speed depend on
    /// both capacitors and manipulators, for example.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<UpgradableVehicleProperty, VehicleUpgrade> AvailableUpgrades = default!;

    /// <summary>
    /// Current modifier applied to the vehicle's speed.
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public float CurrentSpeedModifier = 1.0f;
}

[DataDefinition]
public partial struct VehicleUpgrade
{
    /// <summary>
    /// The machine part used for this upgrade.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MachinePartPrototype> PartType;

    /// <summary>
    /// The upgrade multiplier per tier. These are factors applied to the base
    /// value of the target property. This list should normally contain five
    /// values (none [unused], basic, advanced, super, bluespace).
    /// </summary>
    [DataField(required: true)]
    public List<float> UpgradePerTier;
}

public enum UpgradableVehicleProperty : byte
{
    Speed,
}
