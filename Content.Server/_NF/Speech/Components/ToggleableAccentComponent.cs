using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Speech.Components;

[RegisterComponent]
public sealed partial class ToggleableAccentComponent : Component
{
    //Plagiarized from VocalComponent
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ToggleAccentActionId = "ActionTogglePreSapienceAccent";
    //This too
    [DataField]
    public EntityUid? ToggleAccentAction;

    [DataField]
    public Type AccentComponentType;

    [DataField]
    public bool IsAccentActive;

    [DataField(customTypeSerializer: typeof(ComponentNameSerializer))]
    public string AccentComponentName;
}
