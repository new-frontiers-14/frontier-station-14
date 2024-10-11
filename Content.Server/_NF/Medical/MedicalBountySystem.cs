
using System.Linq;
using Content.Server._NF.Medical.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Stack;
using Content.Shared._NF.Pirate.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Stacks;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server._NF.Medical;

[Serializable, NetSerializable]
public sealed class RedeemMedicalBountyEvent : EntityEventArgs
{
    public NetEntity RedeemingMachine;

    public RedeemMedicalBountyEvent(NetEntity redeemingMachine)
    {
        RedeemingMachine = redeemingMachine;
    }
}

public sealed partial class MedicalBountySystem : EntitySystem
{
    [Dependency] IRobustRandom _random = default!;
    [Dependency] IPrototypeManager _proto = default!;
    [Dependency] DamageableSystem _damageable = default!;
    [Dependency] BloodstreamSystem _bloodstream = default!;
    [Dependency] SharedContainerSystem _container = default!;
    [Dependency] StackSystem _stack = default!;
    [Dependency] AudioSystem _audio = default!;

    private List<MedicalBountyPrototype> _cachedPrototypes = new();

    public override void Initialize()
    {
        base.Initialize();

        _proto.PrototypesReloaded += OnPrototypesReloaded;

        SubscribeLocalEvent<MedicalBountyComponent, ComponentInit>(InitializeMedicalBounty);
        SubscribeNetworkEvent<RedeemMedicalBountyEvent>(RedeemMedicalBounty); // TODO: handle redemption messages

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
            if (_bloodstream.TryAddToChemicals(entity, soln, bloodstream))
                bountyValueAccum += reagentQuantity * reagentValue.ValuePerPoint;
        }

        component.MaxBountyValue = bountyValueAccum;
    }

    private void RedeemMedicalBounty(RedeemMedicalBountyEvent ev)
    {
        // Check that the entity passed in is a valid medical redeemer
        var uid = EntityManager.GetEntity(ev.RedeemingMachine);

        // Check that the medical redeemer has a valid medical bounty inside
        if (!uid.Valid || !TryComp<MedicalBountyRedeemerComponent>(uid, out var redeemer))
        {
            return;
        }

        if (!_container.TryGetContainer(uid, redeemer.BodyContainer, out var container) ||
            container.ContainedEntities.Count <= 0)
        {
            // TODO: popups for "nothing in tube"
            return;
        }

        // Assumption: only one object can be in the MedicalBountyRedeemer
        EntityUid bountyUid = container.ContainedEntities[0];

        if (!TryComp<MedicalBountyComponent>(bountyUid, out var medicalBounty) ||
            medicalBounty.Bounty == null ||
            !TryComp<DamageableComponent>(bountyUid, out var damageable))
        {
            // TODO: popups for "object has no valid medical bounty"
            return;
        }

        // Check that the entity inside is alive.
        var bounty = medicalBounty.Bounty;
        if (damageable.TotalDamage > bounty.MaximumDamageToRedeem)
        {
            // TODO: popups for "object is too damaged to redeem bounty"
            return;
        }

        // Calculate amount of reward to pay out.
        var bountyPayout = medicalBounty.MaxBountyValue;
        foreach (var (damageType, damageVal) in damageable.Damage.DamageDict)
        {
            if (bounty.DamageSets.ContainsKey(damageType))
            {
                bountyPayout -= (int)(bounty.DamageSets[damageType].PenaltyPerPoint * damageVal);
            }
            else
            {
                bountyPayout -= (int)(bounty.PenaltyPerOtherPoint * damageVal);
            }
        }

        // Spawn cash on the machine.
        if (bountyPayout > 0)
        {
            _stack.Spawn(bountyPayout, new ProtoId<StackPrototype>("Credit"), Transform(uid).Coordinates);
        }

        // Delete entity inside the machine.
        QueueDel(bountyUid);

        // Play a kaching noise
        _audio.PlayPvs("/Audio/Effects/Cargo/ping.ogg", uid); // TODO: move to component
    }
}
