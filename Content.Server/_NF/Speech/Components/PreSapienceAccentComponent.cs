using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Speech.Components;

[RegisterComponent]
public sealed partial class PreSapienceAccentComponent : Component
{
    [DataField]
    public Component PreSapienceAccentComp;

    //Plagiarized from VocalComponent
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ToggleAccentActionId = "ActionTogglePreSapienceAccent";

    [DataField]
    public EntityUid? ToggleAccentAction;
}
