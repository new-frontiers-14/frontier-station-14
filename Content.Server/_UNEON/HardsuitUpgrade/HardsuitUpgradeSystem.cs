using System.Linq;
using Content.Server.Armor;
using Content.Server.Containers;
using Content.Server.Materials;
using Content.Server.Power.Components;
using Content.Shared._UNEON.HardsuitUpgrade;
using Content.Shared.ActionBlocker;
using Content.Shared.Armor;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Construction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.Materials.OreSilo;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._UNEON.HardsuitUpgrade;

public sealed class HardsuitUpgradeSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private MaterialStorageSystem _materials = default!;
    [Dependency] private OreSiloSystem _silos = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ArmorSystem _armor = default!;
    [Dependency] private SharedExplosionSystem _explosions = default!;
    [Dependency] private ClothingSpeedModifierSystem _speed = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    private static readonly ProtoId<TagPrototype> HardsuitTag = "Hardsuit";
    private float _uiTimer;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HardsuitUpgradeComponent, HardsuitUpgradeStartMessage>(OnStart);
        SubscribeLocalEvent<HardsuitUpgradeComponent, HardsuitUpgradeEjectMessage>(OnEject);
        SubscribeLocalEvent<HardsuitUpgradeComponent, HardsuitUpgradeMaterialsMessage>(OnEjectMaterials);
        SubscribeLocalEvent<HardsuitUpgradeComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<HardsuitUpgradeComponent, EntRemovedFromContainerMessage>(OnRemoved);
        // Refund paid work before standard deconstruction ejects stored resources and the suit.
        SubscribeLocalEvent<HardsuitUpgradeComponent, MachineDeconstructedEvent>(OnDeconstruct,
            before: new[] { typeof(MaterialStorageSystem), typeof(EmptyOnMachineDeconstructSystem) });
        SubscribeLocalEvent<HardsuitUpgradeComponent, DestructionEventArgs>(OnDestruction,
            before: new[] { typeof(MaterialStorageSystem), typeof(ItemSlotsSystem) });
    }

    public bool IsHardsuit(EntityUid item) => _tags.HasTag(item, HardsuitTag) &&
        HasComp<ArmorComponent>(item) && TryComp<ClothingComponent>(item, out var clothing) &&
        (clothing.Slots & SlotFlags.OUTERCLOTHING) != 0;

    private EntityUid? Suit(EntityUid uid) => _slots.GetItemOrNull(uid, HardsuitUpgradeRules.Slot);
    private bool Powered(EntityUid uid) => Transform(uid).Anchored &&
        TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered;

    private bool CanUse(EntityUid uid, EntityUid actor) =>
        _ui.GetActors(uid, HardsuitUpgradeUiKey.Key).Contains(actor) &&
        _interaction.InRangeUnobstructed(actor, uid) && _blocker.CanInteract(actor, uid);

    private void OnStart(EntityUid uid, HardsuitUpgradeComponent comp, HardsuitUpgradeStartMessage args)
    {
        if (CanUse(uid, args.Actor))
            TryStartUpgrade(uid, args.Kind);
        UpdateUi(uid, comp);
    }

    private void OnEject(EntityUid uid, HardsuitUpgradeComponent comp, HardsuitUpgradeEjectMessage args)
    {
        if (comp.Job != null || !CanUse(uid, args.Actor))
            return;
        if (_slots.TryGetSlot(uid, HardsuitUpgradeRules.Slot, out var slot))
            _slots.TryEjectToHands(uid, slot, args.Actor);
        UpdateUi(uid, comp);
    }

    private void OnEjectMaterials(EntityUid uid, HardsuitUpgradeComponent comp, HardsuitUpgradeMaterialsMessage args)
    {
        if (comp.Job != null || !CanUse(uid, args.Actor))
            return;
        _materials.EjectAllMaterial(uid, localOnly: true);
        UpdateUi(uid, comp);
    }

    private void OnInsertAttempt(Entity<HardsuitUpgradeComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Container.ID == HardsuitUpgradeRules.Slot && (ent.Comp.Job != null || !IsHardsuit(args.EntityUid)))
            args.Cancel();
    }

    private void OnRemoved(EntityUid uid, HardsuitUpgradeComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == HardsuitUpgradeRules.Slot && comp.Job != null)
            CancelAndRefund(uid, comp);
    }

    private void OnDeconstruct(EntityUid uid, HardsuitUpgradeComponent comp, MachineDeconstructedEvent args) =>
        CancelAndRefund(uid, comp);

    private void OnDestruction(EntityUid uid, HardsuitUpgradeComponent comp, DestructionEventArgs args) =>
        CancelAndRefund(uid, comp);

    private void CancelAndRefund(EntityUid uid, HardsuitUpgradeComponent comp)
    {
        var paid = comp.PaidMaterials;
        var siloPaid = comp.PaidSiloMaterials;
        var silo = comp.PaidSilo;
        ClearJob(uid, comp);
        if (siloPaid.Count > 0 && (silo is not { } source || TerminatingOrDeleted(source) ||
            !_materials.TryChangeMaterialAmount((source, null), siloPaid, localOnly: true)))
        {
            foreach (var (material, amount) in siloPaid)
                paid[material] = paid.GetValueOrDefault(material) + amount;
        }
        if (paid.Count > 0 && !Terminating(uid))
            _materials.TryChangeMaterialAmount((uid, null), paid, localOnly: true);
    }

    private void ClearJob(EntityUid uid, HardsuitUpgradeComponent comp)
    {
        comp.Job = null;
        comp.JobSuit = null;
        comp.Remaining = 0;
        comp.Duration = 0;
        comp.PaidMaterials = new();
        comp.PaidSilo = null;
        comp.PaidSiloMaterials = new();
        _slots.SetLock(uid, HardsuitUpgradeRules.Slot, false);
        _appearance.SetData(uid, HardsuitUpgradeVisuals.Working, false);
    }

    public float Current(EntityUid suit, HardsuitUpgradeKind kind)
    {
        if (HardsuitUpgradeRules.DamageType(kind) is { } damage && TryComp<ArmorComponent>(suit, out var armor))
            return HardsuitUpgradeRules.Protection(armor.Modifiers.Coefficients.GetValueOrDefault(damage, 1));
        if (kind == HardsuitUpgradeKind.Explosion)
            return TryComp<ExplosionResistanceComponent>(suit, out var explosion)
                ? HardsuitUpgradeRules.Protection(explosion.DamageCoefficient) : 0;
        if (kind == HardsuitUpgradeKind.Speed)
            return TryComp<ClothingSpeedModifierComponent>(suit, out var speed)
                ? MathF.Max(0, HardsuitUpgradeRules.Protection(MathF.Min(speed.WalkModifier, speed.SprintModifier))) : 0;
        return 0;
    }

    private Dictionary<string, int> Volumes(Dictionary<string, int> quantities) => quantities.ToDictionary(
        pair => pair.Key, pair => pair.Value * _materials.GetSheetVolume(_prototypes.Index<MaterialPrototype>(pair.Key)));

    /// <summary>All costs, limits and the inserted suit are resolved on the server, never supplied by the UI.</summary>
    public bool TryStartUpgrade(EntityUid uid, HardsuitUpgradeKind kind)
    {
        if (!Enum.IsDefined(kind) || !TryComp<HardsuitUpgradeComponent>(uid, out var comp) ||
            comp.Job != null || !Powered(uid) || Suit(uid) is not { } suit || !IsHardsuit(suit))
            return false;
        var current = Current(suit, kind);
        if (!HardsuitUpgradeRules.CanImprove(kind, current))
            return false;
        var target = HardsuitUpgradeRules.Target(kind, current);
        var cost = Volumes(HardsuitUpgradeRules.Cost(kind, target));
        var stored = _materials.GetStoredMaterials((uid, null));
        if (cost.Any(p => stored.GetValueOrDefault(p.Key) < p.Value))
            return false;
        var local = _materials.GetStoredMaterials((uid, null), localOnly: true);
        var localPaid = new Dictionary<string, int>();
        var siloPaid = new Dictionary<string, int>();
        foreach (var (material, amount) in cost)
        {
            var fromLocal = Math.Min(amount, local.GetValueOrDefault(material));
            if (fromLocal > 0)
                localPaid.Add(material, fromLocal);
            if (fromLocal < amount)
                siloPaid.Add(material, amount - fromLocal);
        }
        var silo = TryComp<OreSiloClientComponent>(uid, out var client) ? client.Silo : null;
        if (siloPaid.Count > 0 && (silo is not { } source || !_silos.CanTransmitMaterials(source, uid) ||
            !Debit(source, siloPaid)))
            return false;
        if (localPaid.Count > 0 && !Debit(uid, localPaid))
        {
            if (silo is { } refundSource && siloPaid.Count > 0)
                _materials.TryChangeMaterialAmount((refundSource, null), siloPaid, localOnly: true);
            return false;
        }
        comp.Job = kind;
        comp.JobSuit = suit;
        comp.Target = target;
        comp.Duration = comp.Remaining = HardsuitUpgradeRules.Seconds(kind, target);
        comp.PaidMaterials = localPaid;
        comp.PaidSilo = siloPaid.Count > 0 ? silo : null;
        comp.PaidSiloMaterials = siloPaid;
        _slots.SetLock(uid, HardsuitUpgradeRules.Slot, true);
        _appearance.SetData(uid, HardsuitUpgradeVisuals.Working, true);
        UpdateUi(uid, comp);
        return true;
    }

    private bool Debit(EntityUid uid, Dictionary<string, int> payment) =>
        _materials.TryChangeMaterialAmount((uid, null), payment.ToDictionary(p => p.Key, p => -p.Value), localOnly: true);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _uiTimer += frameTime;
        var refresh = _uiTimer >= 0.25f;
        if (refresh)
            _uiTimer = 0;
        var query = EntityQueryEnumerator<HardsuitUpgradeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Job is { } kind)
            {
                var powered = Powered(uid);
                _appearance.SetData(uid, HardsuitUpgradeVisuals.Working, powered);
                if (Suit(uid) != comp.JobSuit || comp.JobSuit is not { } suit || Deleted(suit))
                    CancelAndRefund(uid, comp);
                else if (powered)
                {
                    comp.Remaining = MathF.Max(0, comp.Remaining - frameTime);
                    if (comp.Remaining <= 0)
                    {
                        Apply(suit, kind, comp.Target);
                        ClearJob(uid, comp);
                        // Eject the very same entity, retaining contents, ownership and appearance.
                        _slots.TryEject(uid, HardsuitUpgradeRules.Slot, null, out _);
                        UpdateUi(uid, comp);
                    }
                }
            }
            if (refresh && _ui.IsUiOpen(uid, HardsuitUpgradeUiKey.Key))
                UpdateUi(uid, comp);
        }
    }

    private void Apply(EntityUid suit, HardsuitUpgradeKind kind, float target)
    {
        if (HardsuitUpgradeRules.DamageType(kind) is { } damage && TryComp<ArmorComponent>(suit, out var armor))
        {
            var coefficient = MathF.Min(armor.Modifiers.Coefficients.GetValueOrDefault(damage, 1), 1 - target / 100);
            _armor.SetDamageCoefficient((suit, armor), damage, coefficient);
        }
        else if (kind == HardsuitUpgradeKind.Explosion)
        {
            var explosion = EnsureComp<ExplosionResistanceComponent>(suit);
            _explosions.SetDamageCoefficient((suit, explosion), MathF.Min(explosion.DamageCoefficient, 1 - target / 100));
        }
        else if (kind == HardsuitUpgradeKind.Speed && TryComp<ClothingSpeedModifierComponent>(suit, out var speed))
            _speed.ReduceClothingSlowdown((suit, speed), HardsuitUpgradeRules.Step / 100);
    }

    private void UpdateUi(EntityUid uid, HardsuitUpgradeComponent comp)
    {
        var suit = Suit(uid);
        var stored = _materials.GetStoredMaterials((uid, null));
        var local = _materials.GetStoredMaterials((uid, null), localOnly: true);
        var silo = TryComp<OreSiloClientComponent>(uid, out var client) ? client.Silo : null;
        var options = new List<HardsuitUpgradeOption>();
        if (suit is { } item && IsHardsuit(item))
        {
            foreach (var kind in Enum.GetValues<HardsuitUpgradeKind>())
            {
                var current = Current(item, kind);
                var target = HardsuitUpgradeRules.Target(kind, current);
                var affordable = Volumes(HardsuitUpgradeRules.Cost(kind, target))
                    .All(p => stored.GetValueOrDefault(p.Key) >= p.Value);
                options.Add(new(kind, current, target, affordable, HardsuitUpgradeRules.CanImprove(kind, current)));
            }
        }
        var quantities = stored.ToDictionary(p => p.Key.ToString(), p =>
            p.Value / Math.Max(1, _materials.GetSheetVolume(_prototypes.Index(p.Key))));
        _ui.SetUiState(uid, HardsuitUpgradeUiKey.Key, new HardsuitUpgradeState(GetNetEntity(suit),
            suit is { } s ? Name(s) : null, Powered(uid), comp.Job != null, comp.Remaining, comp.Duration, options, quantities,
            local.Any(p => p.Value >= _materials.GetSheetVolume(_prototypes.Index(p.Key))),
            silo != null, silo is { } source && _silos.CanTransmitMaterials(source, uid)));
    }
}
