using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Materials;
using Robust.Shared.Prototypes;

[RegisterComponent]
public sealed partial class NFbiomassEqualizerComponent : Component
{
    /// <summary>
    /// The material that produce is converted into
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> ExtractedMaterial = "Biomass";

    /// <summary>
    /// List of reagents that determines how much material is yielded from a produce.
    /// </summary>
    [DataField]
    public List<ProtoId<ReagentPrototype>> ExtractionReagents = new()
    {
        "Nutriment"
    };
}
