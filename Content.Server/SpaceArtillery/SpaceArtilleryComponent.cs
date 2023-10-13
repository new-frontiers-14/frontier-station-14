namespace Content.Shared.SpaceArtillery;
using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Content.Shared.Actions;
using Robust.Shared.Utility;

[RegisterComponent]
public sealed partial class SpaceArtilleryComponent : Component
{
	// Whether the space artillery has enough power
    [ViewVariables(VVAccess.ReadWrite)] public bool IsPowered = false;
	
	// Whether the space artillery need power to operate
    [ViewVariables(VVAccess.ReadWrite)] public bool IsPowerRequired = true;
	
	// Whether the space artillery need power to operate
    [ViewVariables(VVAccess.ReadWrite)] public bool IsArmed = false;
	
    /// <summary>
    /// The current amount of power being used.
    /// </summary>
    [DataField("powerUseActive")]
    public int PowerUseActive = 600;
	
    /// <summary>
    /// Signal port that makes space artillery fire.
    /// </summary>
    [DataField("spaceArtilleryFirePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtilleryFirePort = "SpaceArtilleryFire";
	
    /// <summary>
    /// Signal port that toggles artillery's safety, which is the combat mode
    /// </summary>
    [DataField("spaceArtillerySafetyPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtillerySafetyPort = "SpaceArtillerySafety";

    /// <summary>
    /// The action for firing the artillery when mounted
    /// </summary>

    [DataField("fireAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FireAction = "ActionSpaceArtilleryFire";

    /// <summary>
    /// The action for the horn (if any)
    /// </summary>
    [DataField("fireActionEntity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? FireActionEntity;
}