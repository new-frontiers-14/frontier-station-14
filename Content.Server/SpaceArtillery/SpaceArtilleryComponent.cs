namespace Content.Shared.SpaceArtillery;
using Content.Shared.Construction.Prototypes;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

[RegisterComponent]
public sealed class SpaceArtilleryComponent : Component
{
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
    [DataField("fireAction")]
    [ViewVariables(VVAccess.ReadWrite)]
    public InstantAction FireAction = new()
    {
        UseDelay = TimeSpan.FromSeconds(0.1),
        Icon = new SpriteSpecifier.Texture(new("Objects/Fun/bikehorn.rsi/icon.png")),
        DisplayName = "action-name-honk", //To be changed
        Description = "action-desc-honk", //To be changed
        Event = new FireActionEvent(),
    };
}