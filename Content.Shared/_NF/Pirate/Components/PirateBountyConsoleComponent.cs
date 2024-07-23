using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Pirate.Components;

[RegisterComponent]
public sealed partial class PirateBountyConsoleComponent : Component
{
    /// <summary>
    /// The id of the label entity spawned by the print label button.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BountyLabelId = "PaperPirateBountyManifest"; // TODO: make some paper 
    /// <summary>
    /// The id of the label entity spawned by the print label button.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BountyCrateId = "CratePirateBounty"; // TODO: make some paper 

    /// <summary>
    /// The time at which the console will be able to print a label again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPrintTime = TimeSpan.Zero;

    /// <summary>
    /// The time between prints.
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// The sound made when bounty skipping is denied due to lacking access.
    /// </summary>
    [DataField]
    public SoundSpecifier SpawnChestSound = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");

    /// <summary>
    /// The sound made when the bounty is skipped.
    /// </summary>
    [DataField]
    public SoundSpecifier SkipSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// The sound made when bounty skipping is denied due to lacking access.
    /// </summary>
    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_two.ogg");
}

[NetSerializable, Serializable]
public sealed class PirateBountyConsoleState : BoundUserInterfaceState
{
    public List<PirateBountyData> Bounties;
    public TimeSpan UntilNextSkip;

    public PirateBountyConsoleState(List<PirateBountyData> bounties, TimeSpan untilNextSkip)
    {
        Bounties = bounties;
        UntilNextSkip = untilNextSkip;
    }
}

//TODO: inherit this from the base message
[Serializable, NetSerializable]
public sealed class PirateBountyAcceptMessage : BoundUserInterfaceMessage
{
    public string BountyId;

    public PirateBountyAcceptMessage(string bountyId)
    {
        BountyId = bountyId;
    }
}

[Serializable, NetSerializable]
public sealed class PirateBountySkipMessage : BoundUserInterfaceMessage
{
    public string BountyId;

    public PirateBountySkipMessage(string bountyId)
    {
        BountyId = bountyId;
    }
}
