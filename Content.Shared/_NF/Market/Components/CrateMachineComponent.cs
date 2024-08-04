using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedMarketSystem))]
public sealed partial class CrateMachineComponent: Component
{

    [NonSerialized]
    public List<MarketData> ItemsToSpawn;

    [NonSerialized]
    public bool DidTakeCrate = true;

    [ViewVariables, DataField]
    public bool Powered;

    /// <summary>
    /// Sounds played when the door is opening and crate coming out.
    /// </summary>
    [ViewVariables]
    public SoundSpecifier? OpeningSound = new SoundPathSpecifier("/Audio/Machines/disposalflush.ogg");

    /// <summary>
    /// Sounds played when the door is closing
    /// </summary>
    [ViewVariables]
    public SoundSpecifier? ClosingSound = new SoundPathSpecifier("/Audio/Machines/disposalflush.ogg");

    [DataField]
    public string CratePrototype = "CrateGenericSteel";

    /// <summary>
    /// How long the opening animation will play
    /// </summary>
    [NonSerialized]
    public float OpeningTime = 3.2f;

    /// <summary>
    /// How long the closing animation will play
    /// </summary>
    [NonSerialized]
    public float ClosingTime = 3.2f;

    /// <summary>
    /// Remaining time of opening animation
    /// </summary>
    [NonSerialized]
    public float OpeningTimeRemaining;

    /// <summary>
    /// Remaining time of closing animation
    /// </summary>
    [NonSerialized]
    public float ClosingTimeRemaining;

    #region Graphics

    /// <summary>
    /// The sprite state used to animate the airlock frame when the airlock opens
    /// </summary>
    [DataField]
    public string OpeningSpriteState = "opening";

    /// <summary>
    /// The sprite state used to animate the airlock frame when the airlock closes.
    /// </summary>
    [DataField]
    public string ClosingSpriteState = "closing";

    /// <summary>
    /// The sprite state used to animate the crate going up.
    /// </summary>
    [DataField]
    public string CrateSpriteState = "crate";

    /// <summary>
    /// The sprite state used for the open airlock lights.
    /// </summary>
    [DataField]
    public string OpenSpriteState = "open";

    /// <summary>
    /// The sprite state used for the closed airlock.
    /// </summary>
    [DataField]
    public string ClosedSpriteState = "opening";

    [Serializable, NetSerializable]
    public enum CrateMachineVisuals : byte
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum CrateMachineVisualState : byte
    {
        Open,
        Closed,
        Opening,
        Closing
    }

    #endregion
}
