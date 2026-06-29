using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Addiction;

[RegisterComponent]
public sealed partial class AddictionModifierComponent : Component
{
    /// <summary>
    /// How fast or slow this entity gets addicted to anything compared to others
    /// </summary>
    [DataField, ViewVariables]
    public float Multiplier = 1f;

    /// <summary>
    /// Mapping of multipliers to use for specific addiction types
    /// </summary>
    [DataField, ViewVariables]
    public Dictionary<ProtoId<AddictionPrototype>, float> Modifiers = new();
}

