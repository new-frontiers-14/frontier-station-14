using Content.Server.Speech.Prototypes;
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
    public bool IsAccentActive;

    [DataField(customTypeSerializer: typeof(ComponentNameSerializer))]
    public string AccentComponentName;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string? ReplacementAccentPrototypeName = null;


    /// <summary>
    /// What should happen to the accent if this component were removed.
    /// <remarks>DEFAULT should never be manually set by the system,
    /// it should only be used if a RemovalBehavior was not set by something else.</remarks>
    /// </summary>
    public enum OnRemovalBehavior
    {
        ADD, //If the component is removed, the accent will be added
        REMOVE, //If the component is removed, the accent will be removed
        DEFAULT //No action will be taken if the component is removed. This should never be set manually
    }
}
