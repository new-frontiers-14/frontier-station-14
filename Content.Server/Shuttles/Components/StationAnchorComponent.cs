using Content.Server.Shuttles.Systems;
using Content.Shared.DeviceLinking; // Frontier
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype; // Frontier

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
[Access(typeof(StationAnchorSystem))]
public sealed partial class StationAnchorComponent : Component
{
    // Frontier: Add ports for linking
    [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OnPort = "On";

    [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OffPort = "Off";

    [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TogglePort = "Toggle";
    // End Frontier

    [DataField("switchedOn")]
    public bool SwitchedOn { get; set; } = true;
}
