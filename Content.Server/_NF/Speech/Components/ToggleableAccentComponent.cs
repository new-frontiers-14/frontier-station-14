using Content.Server.Speech.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Speech.Components;

[RegisterComponent]
public sealed partial class ToggleableAccentComponent : Component
{

    [DataField]
    public EntityUid? ToggleAccentAction;

    [DataField]
    public bool IsAccentActive;

    [DataField(customTypeSerializer: typeof(ComponentNameSerializer))]
    public string AccentComponentName;

    [DataField]
    public ProtoId<ReplacementAccentPrototype>? ReplacementAccentPrototypeName = null;

    [DataField]
    public OnRemovalBehavior RemovalBehavior;

    /// <summary>
    /// What should happen to the accent if this component were removed.
    /// </summary>
    public enum OnRemovalBehavior
    {
        Add, //If the component is removed, the accent will be added
        Remove, //If the component is removed, the accent will be removed
    }
}
