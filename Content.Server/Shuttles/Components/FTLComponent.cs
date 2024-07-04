using Content.Shared.Shuttles.Systems;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to a component when it is queued or is travelling via FTL.
/// </summary>
[RegisterComponent]
public sealed partial class FTLComponent : Component
{
    // TODO Full game save / add datafields

    [ViewVariables]
    public FTLState State = FTLState.Available;

    [ViewVariables(VVAccess.ReadWrite)]
    public StartEndTime StateTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public float StartupTime = 0f;

    // Because of sphagetti, actual travel time is Math.Max(TravelTime, DefaultArrivalTime)
    [ViewVariables(VVAccess.ReadWrite)]
    public float TravelTime = 0f;

    /// <summary>
    /// Coordinates to arrive it: May be relative to another grid (for docking) or map coordinates.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public EntityCoordinates TargetCoordinates;

    [DataField]
    public Angle TargetAngle;

    /// <summary>
    /// If we're docking after FTL what is the prioritised dock tag (if applicable).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public ProtoId<TagPrototype>? PriorityTag;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundTravel")]
    public SoundSpecifier? TravelSound = new SoundPathSpecifier("/Audio/Effects/Shuttle/hyperspace_progress.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f).WithLoop(true)
    };

    [DataField]
    public EntityUid? StartupStream;

    [DataField]
    public EntityUid? TravelStream;
}
