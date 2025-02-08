using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server._NF.CryoSleep;
[RegisterComponent]
public sealed partial class CryoSleepComponent : Component
{
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// The sound that is played when a player leaves the game via cryo
    /// </summary>
    [DataField("leaveSound")]
    public SoundSpecifier LeaveSound = new SoundPathSpecifier("/Audio/Effects/radpulse1.ogg");

    /// <summary>
    ///   The ID of the latest DoAfter event associated with this entity. May be null if there's no DoAfter going on.
    /// </summary>
    public DoAfterId? CryosleepDoAfter = null;
}
