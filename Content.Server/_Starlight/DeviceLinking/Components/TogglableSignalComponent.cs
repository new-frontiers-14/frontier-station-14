using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.DeviceLinking.Components;

[RegisterComponent]
public sealed partial class ToggleableSignalComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";
}
