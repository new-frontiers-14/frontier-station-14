using Content.Server.Speech.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Speech.Components;

[RegisterComponent]
public sealed partial class ToggleableAccentComponent : Component
{
    //Plagiarized from VocalComponent
    //TODO: Fix this so it has the proper prototype name, or make it a arg for the MakeAccentTogglable or whatever
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ToggleAccentActionId = "ActionToggleAccent";
    //This too
    [DataField]
    public EntityUid? ToggleAccentAction;

    [DataField]
    public bool IsAccentActive;

    [DataField(customTypeSerializer: typeof(ComponentNameSerializer))]
    public string AccentComponentName;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    public string? ReplacementAccentPrototypeName = null;

    [DataField]
    public OnRemovalBehavior RemovalBehavior;

    /// <summary>
    /// What should happen to the accent if this component were removed.
    /// </summary>
    public enum OnRemovalBehavior
    {
        ADD, //If the component is removed, the accent will be added
        REMOVE, //If the component is removed, the accent will be removed
    }
}
