using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
// using Content.Shared.Magic.Events;

// using Content.Server._Park.Species.Shadowkin.Systems;


namespace Content.Shared._Park.Species.Shadowkin.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShadowkinTeleportPowerComponent : Component
{
    [DataField]
    public EntProtoId TeleportAction = "ShadowkinTeleport";

    [DataField("TeleportActionEntity"), AutoNetworkedField]
    public EntityUid? TeleportActionEntity;


}

public sealed partial class ShadowkinTeleportEvent : WorldTargetActionEvent {
    
    [DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Park/Effects/Shadowkin/Powers/teleport.ogg");

    [DataField("volume")]
    public float Volume = 5f;


    [DataField("powerCost")]
    public float PowerCost = 40f;

    [DataField("staminaCost")]
    public float StaminaCost = 20f;


    [DataField("speech")]
    public string? Speech { get; private set; }


}
