using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market.Components;

[NetworkedComponent]
public abstract partial class SharedCrateMachineComponent: Component
{
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
    /// How long the opening or closing animation will play
    /// </summary>
    [DataField]
    public TimeSpan OpenCloseTime = TimeSpan.FromSeconds(3f);

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
