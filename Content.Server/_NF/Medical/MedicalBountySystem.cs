
using System.Linq;
using Content.Server._NF.Medical.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._NF.Pirate.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Medical;

public sealed partial class MedicalBountySystem : EntitySystem
{
    [Dependency] IRobustRandom _random = default!;
    [Dependency] IPrototypeManager _proto = default!;
    [Dependency] DamageableSystem _damageable = default!;
    [Dependency] BloodstreamSystem _bloodstream = default!;

    private List<MedicalBountyPrototype> _cachedPrototypes = new();

    public override void Initialize()
    {
        base.Initialize();

        _proto.PrototypesReloaded += OnPrototypesReloaded;

        SubscribeLocalEvent<MedicalBountyComponent, ComponentInit>(InitializeMedicalBounty);
        //SubscribeLocalEvent<MedicalBountyComponent, >(RedeemMedicalBounty); // TODO: handle redemption messages

        CacheBountyPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.ByType.ContainsKey(typeof(MedicalBountyPrototype))
            || (args.Removed?.ContainsKey(typeof(MedicalBountyPrototype)) ?? false))
        {
            CacheBountyPrototypes();
        }
    }

    private void CacheBountyPrototypes()
    {
        _cachedPrototypes = _proto.EnumeratePrototypes<MedicalBountyPrototype>().ToList();
    }

    private void InitializeMedicalBounty(EntityUid entity, MedicalBountyComponent component, ComponentInit args)
    {
        if (component.Bounty == null)
        {
            if (_cachedPrototypes.Count > 0)
                component.Bounty = _random.Pick(_cachedPrototypes);
            else
                return; // Nothing to do, keep bounty at null.
        }

        // Precondition: check entity can fulfill bounty conditions
        if (!TryComp<DamageableComponent>(entity, out var damageable) ||
            !TryComp<SolutionContainerManagerComponent>(entity, out var solutionContainer) ||
            !TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        // Apply damage from prototype, keep track of value
        DamageSpecifier damageToApply = new DamageSpecifier();
        var bountyValueAccum = component.Bounty.BaseReward;
        foreach (var (damageType, damageValue) in component.Bounty.DamageSets)
        {
            if (!_proto.TryIndex<DamageTypePrototype>(damageType, out var damageProto))
                continue;

            var randomDamage = _random.Next(damageValue.MinDamage, damageValue.MaxDamage + 1);
            bountyValueAccum += randomDamage * damageValue.ValuePerPoint;
            damageToApply += new DamageSpecifier(damageProto, randomDamage);
        }
        _damageable.SetDamage(entity, damageable, damageToApply);

        // Inject reagents into chemical solution, if any
        foreach (var (reagentType, reagentValue) in component.Bounty.Reagents)
        {
            if (!_proto.HasIndex<ReagentPrototype>(reagentType))
                continue;

            Solution soln = new Solution();
            var reagentQuantity = _random.Next(reagentValue.MinQuantity, reagentValue.MaxQuantity + 1);
            soln.AddReagent(reagentType, reagentQuantity);
            if(_bloodstream.TryAddToChemicals(entity, soln))
                bountyValueAccum += reagentQuantity * reagentValue.ValuePerPoint;
        }

        component.MaxBountyValue = bountyValueAccum;
    }
}
