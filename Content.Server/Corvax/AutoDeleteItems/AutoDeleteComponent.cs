using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

[RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AutoDeleteComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsHumanoidNear;

    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsSSDNear = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ReadyToDelete = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextTimeToDelete;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextTimeToCheck;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DelayToCheck;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DelayToDelete;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DistanceToCheck = 1;
}
