using Content.Shared._CitadelStation.ERP.Examination.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared._CitadelStation.ERP.Examination.Components;

public enum ERPStatus
{
    Prohibited = 0,
    Partial = 1,
    Complete = 2,
    Completer = 3
};
[RegisterComponent, Access(typeof(ERPExaminationSystem))]
//[RegisterComponent]
public sealed partial class ERPExaminableComponent : Component
{
    [DataField(required: false)]
    public ERPStatus ERPAccessLevel = ERPStatus.Partial;
}
