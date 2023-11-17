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
	/// <summary>
	/// Whether the space artillery's safety is enabled or not
	/// </summary>
    [DataField("isArmed"),ViewVariables(VVAccess.ReadWrite)] public bool IsArmed = false;
	
	/// <summary>
	/// Whether the space artillery has enough power
	/// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool IsPowered = false;
	
	/// <summary>
	/// Whether the space artillery's battery is being charged
	/// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool IsCharging = false;
	
	/// <summary>
    /// Rate of charging the battery
    /// </summary>
    [DataField("powerChargeRate"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerChargeRate = 3000;
	
	/// <summary>
	/// Whether the space artillery need power to operate remotely from signal
	/// </summary>
    [DataField("isPowerRequiredForSignal"),ViewVariables(VVAccess.ReadWrite)] public bool IsPowerRequiredForSignal = true;
	
	/// <summary>
	/// Whether the space artillery need power to operate manually when mounted/buckled to
	/// </summary>
    [DataField("isPowerRequiredForMount"),ViewVariables(VVAccess.ReadWrite)] public bool IsPowerRequiredForMount = false;
	
    /// <summary>
    /// Amount of power being used when operating
    /// </summary>
    [DataField("powerUsePassive"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerUsePassive = 600;
	
	/// <summary>
	/// Whether the space artillery needs power to fire a shot
	/// </summary>
    [DataField("isPowerRequiredToFire"),ViewVariables(VVAccess.ReadWrite)] public bool IsPowerRequiredToFire = false;
	
	/// <summary>
    /// Amount of power used when firing
    /// </summary>
    [DataField("powerUseActive"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerUseActive = 6000;
	

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