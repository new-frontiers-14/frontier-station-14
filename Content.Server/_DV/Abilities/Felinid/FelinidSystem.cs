using Content.Server.Body.Components;
using Content.Server.Medical;
using Content.Shared._DV.Abilities;
using Content.Shared._DV.Abilities.Felinid;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Item;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Server._DV.Abilities.Felinid;

/// <summary>
/// Handles felinid logic except for fitting in bags.
/// </summary>
/// <remarks>
/// This could be moved to shared if:
/// 1. bloodstream was in shared
/// 2. vomiting was in shared
/// 3. this didn't use RNG.
/// </remarks>
public sealed class FelinidSystem : SharedFelinidSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidComponent, ItemCoughedUpEvent>(OnItemCoughedUp);

        SubscribeLocalEvent<HairballComponent, ThrowDoHitEvent>(OnHairballHit);
        SubscribeLocalEvent<HairballComponent, GettingPickedUpAttemptEvent>(OnHairballPickupAttempt);
    }

    private void OnItemCoughedUp(Entity<FelinidComponent> ent, ref ItemCoughedUpEvent args)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var blood) || blood.ChemicalSolution is not {} solution)
            return;

        var item = args.Item;
        var hairball = Comp<HairballComponent>(item);
        var purged = _solution.SplitSolution(solution, ent.Comp.PurgedQuantity);
        if (_solution.TryGetSolution(item, hairball.SolutionName, out var hairballSolution))
        {
            _solution.TryAddSolution(hairballSolution.Value, purged);
        }
    }

    private void OnHairballHit(Entity<HairballComponent> ent, ref ThrowDoHitEvent args)
    {
        TryVomit(ent, args.Target);
    }

    private void OnHairballPickupAttempt(Entity<HairballComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (TryVomit(ent, args.User))
            args.Cancel();
    }

    private bool TryVomit(Entity<HairballComponent> ent, EntityUid uid)
    {
        if (HasComp<FelinidComponent>(uid) || !HasComp<StatusEffectsComponent>(uid))
            return false;

        if (!_random.Prob(ent.Comp.VomitProb))
            return false;

        _vomit.Vomit(uid);
        return true;
    }
}
