using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntityEffects;

public sealed partial class DisintegrateArtifact : EntityEffect
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

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;
        var artifact = args.EntityManager.EntitySysManager.GetEntitySystem<ArtifactSystem>();
        artifact.DisintegrateArtifact(reagentArgs.TargetEntity, ProbabilityMin, ProbabilityMax, Range);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        null;
}
