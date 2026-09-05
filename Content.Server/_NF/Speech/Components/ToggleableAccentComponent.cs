using Content.Server._NF.Speech.EntitySystems;
using Content.Server.Speech.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Speech.Components;

[RegisterComponent]
public sealed partial class ToggleableAccentComponent : Component
{

    /// <remarks>
    /// This only exists so it can be added in YML and still be able to init properly
    /// </remarks>
    [DataField]
    public EntProtoId ActionPrototype = ToggleableAccentSystem.GenericToggleAccentPrototypeIdString;

    /// <remarks>
    /// We save the action so we can remove it later.
    /// </remarks>
    [DataField]
    public EntityUid? ToggleAccentAction;

    [DataField]
    public bool IsAccentActive;

    /// <remarks>
    /// This field is initialized to an empty string to allow the system to detect and not explode when the component is added
    /// without being setup completely, like through admin intervention.
    /// </remarks>
    [DataField(customTypeSerializer: typeof(ComponentNameSerializer))]
    public string AccentComponentName = "";

    /// <remarks>
    /// This field is initialized to an empty string to allow the system to detect and not explode when the component is added
    /// without being setup completely, like through admin intervention.
    /// </remarks>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<ReplacementAccentPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ReplacementAccentPrototypeName = "";

    /// <summary>
    /// What should happen to the accent if this component was removed. See OnRemovalBehavior for descriptions of what the
    /// behaviors do.
    /// </summary>
    /// <remarks>
    /// This defaults to remove, but should preferably always be set in YAML. If this comp is added through MakeAccentToggleable,
    /// it will always have this value overriden.
    /// </remarks>>
    [DataField]
    public OnRemovalBehavior RemovalBehavior = OnRemovalBehavior.Remove;


    public enum OnRemovalBehavior
    {
        Add, //If the component is removed, the accent will be added
        Remove, //If the component is removed, the accent will be removed
    }
}
