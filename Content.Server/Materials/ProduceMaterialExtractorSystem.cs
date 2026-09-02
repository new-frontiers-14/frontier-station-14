using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Materials.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;

namespace Content.Server.Materials;

public sealed class ProduceMaterialExtractorSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProduceMaterialExtractorComponent, AfterInteractUsingEvent>(OnInteractUsing);
    }

    // BEGIN Frontier - Cherry pick wizden#32663
    private void OnInteractUsing(Entity<ProduceMaterialExtractorComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!this.IsPowered(ent, EntityManager))
            return;

        bool success = false;

        // Handle using bags (mainly plant bags)
        if (ExtractFromStorage(ent, args.Used, ref args))
            success = true;

        // Handle using produce directly
        if (ExtractFromProduce(ent, args.Used, ref args))
            success = true;

        // TODO: What if a bag is also a plant?

        if (success)
        {
            _audio.PlayPvs(ent.Comp.ExtractSound, ent);
            args.Handled = true;
        }
    }

    private bool ExtractFromProduce(Entity<ProduceMaterialExtractorComponent> ent, EntityUid used, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<ProduceComponent>(used, out var produce))
            return false;

        if (!_solutionContainer.TryGetSolution(used, produce.SolutionName, out var solution))
            return false;

        // Can produce even have fractional amounts? Does it matter if they do?
        // Questions man was never meant to answer.
        var matAmount = solution.Value.Comp.Solution.Contents
            .Where(r => ent.Comp.ExtractionReagents.Contains(r.Reagent.Prototype))
            .Sum(r => r.Quantity.Float());

        var changed = (int)matAmount;

        if (changed == 0)
        {
            _popup.PopupEntity(Loc.GetString("material-extractor-comp-wrongreagent", ("used", args.Used)), args.User, args.User);
            return false; // Frontier TODO: Nuke this file and replace with upstream one once Wizden#32663 gets merged
        }

        _materialStorage.TryChangeMaterialAmount(ent, ent.Comp.ExtractedMaterial, changed);

        QueueDel(used);

        return true;
    }

    private bool ExtractFromStorage(Entity<ProduceMaterialExtractorComponent> ent, EntityUid used, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<StorageComponent>(used, out var storage))
            return false;

        bool success = false;

        foreach (var (item, _location) in storage.StoredItems)
            if (ExtractFromProduce(ent, item, ref args))
                success = true;

        return success;
    }
    // END Frontier
}
