using Content.Server.Shuttles.Systems;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Shuttles.Components;

[RegisterComponent]
[Access(typeof(StationAnchorSystem))]
public sealed partial class StationAnchorComponent : Component
{
    /* New Frontiers - StationAnchor Links - Added the necessary ports for linking.
        This code is licensed under AGPLv3. See AGPLv3.txt */

    [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OnPort = "On";

    [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OffPort = "Off";

    [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TogglePort = "Toggle";

    // End of modified code

    [DataField("switchedOn")]
    public bool SwitchedOn { get; set; } = true;
}
