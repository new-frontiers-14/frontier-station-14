using Content.Shared._CitadelStation.ERP.Interaction.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CitadelStation.ERP.Interaction.Components;

[RegisterComponent, Access(typeof(SharedArousalSystem)), NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ERPArousalComponent : Component {
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 ArousalBaseline = 0.0f;
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 ArousalModifier = 1.0f;
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 CurrentArousal = 0.0f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextMoan = TimeSpan.Zero;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan MoanCoolDown = TimeSpan.FromSeconds(15);
    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 MoanThreshold = 0.8f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(1);

};
