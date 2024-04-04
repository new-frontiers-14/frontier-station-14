using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class DisintegrateArtifact : ReagentEffect
{

    /// <summary>
    /// Disintegrate chance
    /// </summary>
    [DataField("probabilityMin"), ViewVariables(VVAccess.ReadWrite)]
    public float ProbabilityMax = 0.05f;

    /// <summary>
    /// Disintegrate chance
    /// </summary>
    [DataField("probabilityMax"), ViewVariables(VVAccess.ReadWrite)]
    public float ProbabilityMin = 0.15f;

    /// <summary>
    /// The range around the artifact that it will spawn the entity
    /// </summary>
    [DataField("range")]
    public float Range = 0.5f;

    public override void Effect(ReagentEffectArgs args)
    {
        var artifact = args.EntityManager.EntitySysManager.GetEntitySystem<ArtifactSystem>();
        artifact.DisintegrateArtifact(args.SolutionEntity, ProbabilityMin, ProbabilityMax, Range);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        null;
}
