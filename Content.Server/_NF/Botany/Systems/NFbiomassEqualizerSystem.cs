using System.Linq;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.Materials;

public sealed class NFbiomassEqualizerSystem : EntitySystem
{
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public int ExtractMaterial(EntityUid entity)
    {
        if (!TryComp<NFbiomassEqualizerComponent>(entity, out var extractComp))
            return 0;

        if (!_solutionContainer.TryGetSolution(entity, "default", out var solution))
            return 0;

        var matAmount = solution.Value.Comp.Solution.Contents
            .Where(r => extractComp.ExtractionReagents.Contains(r.Reagent.Prototype))
            .Sum(r => r.Quantity.Float());

        _materialStorage.TryChangeMaterialAmount(entity, extractComp.ExtractedMaterial, (int)matAmount);
        return (int)matAmount;
    }
}
