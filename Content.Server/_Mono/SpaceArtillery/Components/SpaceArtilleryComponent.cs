using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Mono.SpaceArtillery.Components;

[RegisterComponent]
public sealed partial class SpaceArtilleryComponent : Component
{

    /// <summary>
    /// Amount of power being used when operating
    /// </summary>
    [DataField("powerUsePassive"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerUsePassive = 600;

    /// <summary>
    /// Rate of charging the battery
    /// </summary>
    [DataField("powerChargeRate"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerChargeRate = 3000;

    /// <summary>
    /// Amount of power used when firing
    /// </summary>
    [DataField("powerUseActive"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerUseActive = 6000;


    ///Sink Ports
    /// <summary>
    /// Signal port that makes space artillery fire.
    /// </summary>
    [DataField("spaceArtilleryFirePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtilleryFirePort = "SpaceArtilleryFire";

}
