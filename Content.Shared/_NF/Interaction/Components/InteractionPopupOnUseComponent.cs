using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Interaction.Components;

/// <summary>
/// A component for RP fluff items.
/// These don't do much mechanically - they display context-sensitive popups for a target
/// display a popup after some amount of time and optionally trigger other things.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InteractionPopupOnUseComponent : Component
{
    /// <summary>
    /// An optional whitelist for entities this can be used on.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The maximum distance this item can be used at.
    /// </summary>
    [DataField]
    public float MaxDistance = 1;

    /// <summary>
    /// Parameters for use on yourself.  Null if cannot be used on yourself.
    /// </summary>
    [DataField]
    public InteractionData? Self;

    /// <summary>
    /// Parameters for use on others.  Null if cannot be used on others.
    /// </summary>
    [DataField]
    public InteractionData? Others;

    /// <summary>
    /// Verb text to show when using.
    /// </summary>
    [DataField]
    public LocId? VerbUse;

    /// <summary>
    /// Sound effect to be played when the interaction succeeds.
    /// Nullable in case no path is specified on the yaml prototype.
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractSuccessSound;

    /// <summary>
    /// Sound effect to be played when the interaction fails.
    /// Nullable in case no path is specified on the yaml prototype.
    /// </summary>
    [DataField]
    public SoundSpecifier? InteractFailureSound;

    /// <summary>
    /// A prototype that will spawn upon successful interaction (as planned only for special effects)
    /// </summary>
    [DataField]
    public EntProtoId? InteractSuccessSpawn;

    /// <summary>
    /// A prototype that will spawn upon failure interaction (as planned only for special effects)
    /// </summary>
    [DataField]
    public EntProtoId? InteractFailureSpawn;

    /// <summary>
    /// Probability (0-1) that an interaction attempt will succeed.
    /// </summary>
    [DataField]
    public float SuccessChance = 1.0f;

    /// <summary>
    /// Will the sound effect be perceived by entities not involved in the interaction?
    /// </summary>
    [DataField]
    public bool SoundPerceivedByOthers = true;
}

[DataDefinition]
public partial record struct InteractionData
{
    /// <summary>
    /// The message to display when starting the doafter.
    /// If UseOnSelfDelay is <= 0, this will not appear.
    /// </summary>
    [DataField]
    public TimeSpan Delay;

    [DataField]
    public LocId? WhitelistFailed;

    [DataField]
    public InteractionMessageSet Actor;

    /// <remarks>
    /// Self-interactions will never invoke the target message set.
    /// </remarks>
    [DataField]
    public InteractionMessageSet Target;

    [DataField]
    public InteractionMessageSet Observers;
}

[DataDefinition]
public partial record struct InteractionMessageSet
{
    /// <summary>
    /// The message to display when starting the doafter.
    /// If UseOnSelfDelay is <= 0, this will not appear.
    /// </summary>
    [DataField]
    public LocId? Start;

    [DataField]
    public LocId? Success;

    [DataField]
    public LocId? Failure;
}

[Serializable, NetSerializable]
public sealed partial class InteractionPopupOnUseDoAfterEvent : SimpleDoAfterEvent;
