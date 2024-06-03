using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype("salvageTimeMod")]
public sealed class SalvageTimeMod : IPrototype, ISalvageMod
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Cost for difficulty modifiers.
    /// Frontier: expedition timer min set to 900 and max to 930
    /// </summary>
    [DataField("cost")]
    public float Cost { get; private set; }

    [DataField("minDuration")]
    public int MinDuration = 900;

    [DataField("maxDuration")]
    public int MaxDuration = 930;
}
