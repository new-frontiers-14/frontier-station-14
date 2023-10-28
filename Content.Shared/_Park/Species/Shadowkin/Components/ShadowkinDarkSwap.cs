using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Park.Species.Shadowkin.Components;

// [RegisterComponent]
// public sealed partial class ShadowkinDarkSwapPowerComponent : Component
// {

//     [DataField]
//     public EntProtoId DarkSwapAction = "ShadowkinDarkSwap";

//     [DataField("DarkSwapActionEntity"), AutoNetworkedField]
//     public EntityUid? DarkSwapActionEntity;

// }

public sealed partial class ShadowkinDarkSwapEvent : InstantActionEvent
{
    [DataField("soundOn")]
    public SoundSpecifier SoundOn = new SoundPathSpecifier("/Audio/_Park/Effects/Shadowkin/Powers/darkswapon.ogg");

    [DataField("volumeOn")]
    public float VolumeOn = 5f;

    [DataField("soundOff")]
    public SoundSpecifier SoundOff = new SoundPathSpecifier("/Audio/_Park/Effects/Shadowkin/Powers/darkswapoff.ogg");

    [DataField("volumeOff")]
    public float VolumeOff = 5f;

    /// <summary>
    ///     How much stamina to drain when darkening.
    /// </summary>
    [DataField("powerCostOn")]
    public float PowerCostOn = 60f;

    /// <summary>
    ///     How much stamina to drain when lightening.
    /// </summary>
    [DataField("powerCostOff")]
    public float PowerCostOff = 45f;

    /// <summary>
    ///     How much stamina to drain when darkening.
    /// </summary>
    [DataField("staminaCostOn")]
    public float StaminaCostOn;

    /// <summary>
    ///     How much stamina to drain when lightening.
    /// </summary>
    [DataField("staminaCostOff")]
    public float StaminaCostOff;


    [DataField("speech")]
    public string? Speech { get; private set; }
}

public sealed partial class ShadowkinDarkSwapAttemptEvent : CancellableEntityEventArgs
{
    [DataField("Performer"), AutoNetworkedField]
    EntityUid Performer;

    public ShadowkinDarkSwapAttemptEvent(EntityUid performer)
    {
        Performer = performer;
    }
}