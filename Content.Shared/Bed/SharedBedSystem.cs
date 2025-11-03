using Content.Shared.Actions;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Construction.Components; // Frontier
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Bed;

public abstract class SharedBedSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly ActionContainerSystem _actConts = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedMetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealOnBuckleComponent, MapInitEvent>(OnHealMapInit);
        SubscribeLocalEvent<HealOnBuckleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<HealOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<StasisBedComponent, StrappedEvent>(OnStasisStrapped);
        SubscribeLocalEvent<StasisBedComponent, UnstrappedEvent>(OnStasisUnstrapped);
        SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnStasisEmagged);
        SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<StasisBedBuckledComponent, GetMetabolicMultiplierEvent>(OnStasisGetMetabolicMultiplier);

        SubscribeLocalEvent<StasisBedComponent, GotUnEmaggedEvent>(OnUnemagged); // Frontier
        SubscribeLocalEvent<StasisBedComponent, RefreshPartsEvent>(OnRefreshParts); // Frontier
        SubscribeLocalEvent<StasisBedComponent, UpgradeExamineEvent>(OnUpgradeExamine); // Frontier
    }

    private void OnHealMapInit(Entity<HealOnBuckleComponent> ent, ref MapInitEvent args)
    {
        _actConts.EnsureAction(ent.Owner, ref ent.Comp.SleepAction, SleepingSystem.SleepActionId);
        Dirty(ent);
    }

    private void OnStrapped(Entity<HealOnBuckleComponent> bed, ref StrappedEvent args)
    {
        EnsureComp<HealOnBuckleHealingComponent>(bed);
        bed.Comp.NextHealTime = Timing.CurTime + TimeSpan.FromSeconds(bed.Comp.HealTime);
        _actionsSystem.AddAction(args.Buckle, ref bed.Comp.SleepAction, SleepingSystem.SleepActionId, bed);
        Dirty(bed);

        // Single action entity, cannot strap multiple entities to the same bed.
        DebugTools.AssertEqual(args.Strap.Comp.BuckledEntities.Count, 1);
    }

    private void OnUnstrapped(Entity<HealOnBuckleComponent> bed, ref UnstrappedEvent args)
    {
        _actionsSystem.RemoveAction(args.Buckle.Owner, bed.Comp.SleepAction);
        _sleepingSystem.TryWaking(args.Buckle.Owner);
        RemComp<HealOnBuckleHealingComponent>(bed);
    }

    private void OnStasisStrapped(Entity<StasisBedComponent> ent, ref StrappedEvent args)
    {
        EnsureComp<StasisBedBuckledComponent>(args.Buckle);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnStasisUnstrapped(Entity<StasisBedComponent> ent, ref UnstrappedEvent args)
    {
        RemComp<StasisBedBuckledComponent>(ent);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnStasisEmagged(Entity<StasisBedComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        ent.Comp.Multiplier = 1f / ent.Comp.Multiplier;
        UpdateMetabolisms(ent.Owner);
        Dirty(ent);

        args.Handled = true;
    }

    // Frontier: demag
    private void OnUnemagged(Entity<StasisBedComponent> ent, ref GotUnEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (!_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        ent.Comp.Multiplier = 1f / ent.Comp.Multiplier; // Reciprocal of reciprocal
        UpdateMetabolisms(ent.Owner);
        Dirty(ent);
        args.Handled = true;
    }
    // End Frontier: demag

    private void OnPowerChanged(Entity<StasisBedComponent> ent, ref PowerChangedEvent args)
    {
        UpdateMetabolisms(ent.Owner);
    }

    private void OnStasisGetMetabolicMultiplier(Entity<StasisBedBuckledComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (!TryComp<BuckleComponent>(ent, out var buckle) || buckle.BuckledTo is not { } buckledTo)
            return;

        if (!TryComp<StasisBedComponent>(buckledTo, out var stasis))
            return;

        if (!_powerReceiver.IsPowered(buckledTo))
            return;

        args.Multiplier *= stasis.Multiplier;
    }

    protected void UpdateMetabolisms(Entity<StrapComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var buckledEntity in ent.Comp.BuckledEntities)
        {
            _metabolizer.UpdateMetabolicMultiplier(buckledEntity);
        }
    }

    // Frontier: upgradeable parts
    private void OnRefreshParts(EntityUid uid, StasisBedComponent component, RefreshPartsEvent args)
    {
        var metabolismRating = args.PartRatings[component.MachinePartMetabolismModifier];
        component.Multiplier = component.BaseMultiplier * metabolismRating; //linear scaling so it's not OP
        if (_emag.CheckFlag(uid, EmagType.Interaction))
            component.Multiplier = 1f / component.Multiplier;
    }

    private void OnUpgradeExamine(EntityUid uid, StasisBedComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("stasis-bed-component-upgrade-stasis", component.Multiplier / component.BaseMultiplier);
    }
    // End Frontier: upgradeable parts
}
