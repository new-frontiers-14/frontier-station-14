using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.VoiceMask;

public enum RadioMode : byte // Frontier 
{
    Real,
    Fake,
    Unknown
}

[RegisterComponent]
public sealed partial class VoiceMaskerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public string LastSetName = "Unknown";

    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Action = "ActionChangeVoiceMask";

    [DataField("actionEntity")] public EntityUid? ActionEntity;

    [ViewVariables(VVAccess.ReadWrite), DataField("radioMode")] // Frontier 
    [AutoNetworkedField]
    public RadioMode RadioMode = RadioMode.Unknown;
}
