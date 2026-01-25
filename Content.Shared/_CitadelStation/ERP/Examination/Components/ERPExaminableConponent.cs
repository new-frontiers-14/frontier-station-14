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
public sealed partial class ERPExaminableComponent : Component
{
    public List<FixedPoint2> ArousalThresholds = new() { FixedPoint2.New(8), FixedPoint2.New(15), FixedPoint2.New(30), FixedPoint2.New(50), FixedPoint2.New(75), FixedPoint2.New(100) };

    [DataField(required: true)]
    public ERPStatus ERPAccessLevel = ERPStatus.Prohibited;
}
