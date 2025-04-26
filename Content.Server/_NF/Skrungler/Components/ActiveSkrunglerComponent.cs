using Robust.Shared.Audio;

namespace Content.Server._NF.Skrungler.Components;

/// <summary>
/// used to track the machine running state
/// </summary>
[RegisterComponent]
public sealed partial class ActiveSkrunglerComponent : Component
{
    [DataField, ViewVariables]
    public float Accumulator = 0;

    [DataField]
    public SoundSpecifier SkrungStartSound = new SoundCollectionSpecifier("gib");

    [DataField]
    public SoundSpecifier SkrunglerSound = new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg");

    [DataField]
    public SoundSpecifier SkrungFinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}
