using Content.Server.Fluids.EntitySystems;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
/// This handles <see cref="ChemicalPuddleArtifactComponent"/>
/// </summary>
public sealed class ChemicalPuddleArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ArtifactSystem _artifact = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    /// <summary>
    /// The key for the node data entry containing
    /// the chemicals that the puddle is made of.
    /// </summary>
    public const string NodeDataChemicalList = "nodeDataChemicalList";
    /// <summary>
    /// Frontier: the key for the node data entry containing
    /// the amount of chemicals spawned so far from this node.
    /// </summary>
    public const string NodeDataVolumeSpawned = "nodeDataVolumeSpawned";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChemicalPuddleArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ChemicalPuddleArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (!TryComp<ArtifactComponent>(uid, out var artifact))
            return;

        if (!_artifact.TryGetNodeData(uid, NodeDataChemicalList, out List<string>? chemicalList, artifact))
        {
            chemicalList = new();
            for (var i = 0; i < component.ChemAmount; i++)
            {
                var chemProto = _random.Pick(component.PossibleChemicals);
                chemicalList.Add(chemProto);
            }

            _artifact.SetNodeData(uid, NodeDataChemicalList, chemicalList, artifact);
        }

        // Frontier: maximum volume per node
        if (!_artifact.TryGetNodeData(uid, NodeDataVolumeSpawned, out FixedPoint2 volumeSpawned))
            volumeSpawned = 0;
        FixedPoint2 volumeToSpawn = FixedPoint2.Min(component.ChemicalSolution.MaxVolume, component.MaximumVolume - volumeSpawned);
        volumeToSpawn = FixedPoint2.Max(0, volumeToSpawn);
        _artifact.SetNodeData(uid, NodeDataVolumeSpawned, volumeSpawned + volumeToSpawn, artifact);

        var amountPerChem = volumeToSpawn / component.ChemAmount;
        // End Frontier
        foreach (var reagent in chemicalList)
        {
            component.ChemicalSolution.AddReagent(reagent, amountPerChem);
        }

        _puddle.TrySpillAt(uid, component.ChemicalSolution, out _);
    }
}
