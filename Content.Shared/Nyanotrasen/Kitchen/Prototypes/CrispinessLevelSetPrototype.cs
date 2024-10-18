// Frontier: prototype for crispiness descriptions.  Kept with other Nyanotrasen deep fryer components for now.
using Content.Shared.Nyanotrasen.Kitchen.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Nyanotrasen.Kitchen.Prototypes;

[Prototype("crispinessLevelSet")]
public sealed partial class CrispinessLevelSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Crispiness level strings. The index is the crispiness value used, starting with 0.
    /// Maximum crispiness is assumed by the size of the list.
    /// </summary>
    [DataField(required: true)] public List<CrispinessTextSet> Levels = new();

    /// <summary>
    /// Shader to use for crispiness settings.
    /// </summary>
    [DataField(required: true)] public DeepFriedVisuals Visual { get; private set; } = default!;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class CrispinessTextSet
{
    // Localized string for name format, should expect to receive "entity" as the name of the entity.
    [DataField(required: true)]
    public string Name = default!;

    // Localized string for examine text, should not receive arguments.
    [DataField(required: true)]
    public string ExamineText = default!;
}
