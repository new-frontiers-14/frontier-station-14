using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

[RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AutoDeleteComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsHumanoidNear = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ReadyToDelete = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextTimeToDelete;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextTimeToCheck;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DelayToCheck = TimeSpan.FromSeconds(10f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DelayToDelete = TimeSpan.FromSeconds(20f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DistanceToCheck = 5;
}
