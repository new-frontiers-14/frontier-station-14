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
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;

[RegisterComponent]
public sealed partial class SpaceArtilleryComponent : Component
{
	[ViewVariables(VVAccess.ReadWrite), DataField("coolantType", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CoolantType = "Coolant";
	
    public static string CoolantSlotSlotId = "SpaceArtillery-CoolantSlot";

    [DataField("SpaceArtillery-CoolantSlot")]
    public ItemSlot CoolantSlot = new();
	
	/// <summary>
	/// Whether the space artillery need coolant to fire on top of ammunition or power
	/// </summary>
    [DataField("isCoolantRequiredToFire"),ViewVariables(VVAccess.ReadWrite)] public bool IsCoolantRequiredToFire = false;
	
	/// <summary>
    /// Stored amount of coolant
    /// </summary>
    [DataField("coolantStored"), ViewVariables(VVAccess.ReadWrite)]
    public int CoolantStored = 0;
	
	/// <summary>
    /// Maximum amount of coolant that can fit
    /// </summary>
    [DataField("maxCoolantStored"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxCoolantStored = 90;
	
	/// <summary>
    /// Amount of coolant consumed upon artillery firing
    /// </summary>
    [DataField("coolantConsumed"), ViewVariables(VVAccess.ReadWrite)]
    public int CoolantConsumed = 1;
	
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
	/// Whether the space artillery requires whole vessel to activate its armaments, 
	/// Use these for any armaments with high destructive capability
	/// </summary>
    [DataField("isDestructive"),ViewVariables(VVAccess.ReadWrite)] public bool IsDestructive = true;
	
	/// <summary>
	/// Whether the space artillery can send signals at all
	/// </summary>
    [DataField("isCapableOfSendingSignal"),ViewVariables(VVAccess.ReadWrite)] public bool IsCapableOfSendingSignal = true;
	
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
    /// Maximum velocity which grid can reach from recoil impulse, at moment there is high variance, will go above it by 10 
    /// </summary>
    [DataField("velocityLimitRecoilGrid"), ViewVariables(VVAccess.ReadWrite)]
    public float VelocityLimitRecoilGrid = 30;
	
	/// <summary>
    /// Amount of power used when firing
    /// </summary>
    [DataField("linearRecoilGrid"), ViewVariables(VVAccess.ReadWrite)]
    public float LinearRecoilGrid = 30;
	
	/// <summary>
    /// Amount of power used when firing
    /// </summary>
    [DataField("angularInstabilityGrid"), ViewVariables(VVAccess.ReadWrite)]
    public float AngularInstabilityGrid = 10;
	
	
	/// <summary>
    /// Maximum velocity which unanchored weapon can reach from recoil impulse
    /// </summary>
    [DataField("velocityLimitRecoilWeapon"), ViewVariables(VVAccess.ReadWrite)]
    public float VelocityLimitRecoilWeapon = 30;
	
	/// <summary>
    /// Amount of power used when firing
    /// </summary>
    [DataField("linearRecoilWeapon"), ViewVariables(VVAccess.ReadWrite)]
    public float LinearRecoilWeapon = 60;
	
	/// <summary>
    /// Amount of power used when firing
    /// </summary>
    [DataField("angularInstabilityWeapon"), ViewVariables(VVAccess.ReadWrite)]
    public float AngularInstabilityWeapon = 30;
	

///Sink Ports
    /// <summary>
    /// Signal port that makes space artillery fire.
    /// </summary>
    [DataField("spaceArtilleryFirePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtilleryFirePort = "SpaceArtilleryFire";
	
    /// <summary>
    /// Signal port that toggles artillery's safety, which is the combat mode
    /// </summary>
    [DataField("spaceArtilleryToggleSafetyPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtilleryToggleSafetyPort = "SpaceArtilleryToggleSafety";

    /// <summary>
    /// Signal port that sets artillery's safety to "SAFE"
    /// </summary>
    [DataField("spaceArtilleryOnSafetyPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtilleryOnSafetyPort = "SpaceArtilleryOnSafety";

    /// <summary>
    /// Signal port that sets artillery's safety to "ARMED"
    /// </summary>
    [DataField("spaceArtilleryOffSafetyPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtilleryOffSafetyPort = "SpaceArtilleryOffSafety";

///Source Ports
    /// <summary>
    /// The port that gets set to high while the alarm is in the danger state, and low when not.
    /// </summary>
    [DataField("spaceArtilleryDetectedFiringPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string SpaceArtilleryDetectedFiringPort = "SpaceArtilleryDetectedFiring";

    /// <summary>
    /// The port that gets set to high while the alarm is in the danger state, and low when not.
    /// </summary>
    [DataField("spaceArtilleryDetectedMalfunctionPort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string SpaceArtilleryDetectedMalfunctionPort = "SpaceArtilleryDetectedMalfunction";

    /// <summary>
    /// The port that gets set to high while the alarm is in the danger state, and low when not.
    /// </summary>
    [DataField("spaceArtilleryDetectedSafetyChangePort", customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string SpaceArtilleryDetectedSafetyChangePort = "SpaceArtilleryDetectedSafetyChange";


///Actions
    /// <summary>
    /// The action for firing the artillery when mounted
    /// </summary>

    [DataField("fireAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? FireAction = "ActionSpaceArtilleryFire";

    /// <summary>
    /// The action for the weapon (if any)
    /// </summary>
    [DataField("fireActionEntity")]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? FireActionEntity;
	
}