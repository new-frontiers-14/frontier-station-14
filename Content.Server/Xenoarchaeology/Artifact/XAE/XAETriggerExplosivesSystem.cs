using Content.Server.Explosion.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Explosion.Components;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect of triggering explosion.
/// </summary>
public sealed class XAETriggerExplosivesSystem : BaseXAESystem<XAETriggerExplosivesComponent>
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly XenoArtifactSystem _xenoArtifact = default!; // Frontier

    /// <inheritdoc />
    protected override void OnActivated(Entity<XAETriggerExplosivesComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        if (!TryComp<ExplosiveComponent>(ent, out var explosiveComp))
            return;
        // Frontier: Scale explosion strength with node complexity
        var predecessorNodes = _xenoArtifact.GetPredecessorNodes(args.Artifact.Owner, args.Node);
        var totalIntensity = Math.Min(explosiveComp.TotalIntensity * (predecessorNodes.Count + 1) / 6, explosiveComp.TotalIntensity);

        _explosion.TriggerExplosive(ent, explosiveComp, explosiveComp.DeleteAfterExplosion ?? false, totalIntensity);
        // End Frontier: Scale explosion strength with node complexity
    }
}
