using Content.Shared.Tools;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Digging.Components;

[RegisterComponent]
public sealed partial class EarthDiggingComponent : Component
{
    [ViewVariables, DataField]
    public bool ToolComponentNeeded = true;

    [ViewVariables, DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
    public string QualityNeeded = "Digging";

    [ViewVariables, DataField]
    public float Delay = 2f;

}
