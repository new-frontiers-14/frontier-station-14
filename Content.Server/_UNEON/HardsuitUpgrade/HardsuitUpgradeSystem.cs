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
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server._UNEON.HardsuitUpgrade;

public sealed class HardsuitUpgradeSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private MaterialStorageSystem _materials = default!;
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
        _materials.EjectAllMaterial(uid);
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
        ClearJob(uid, comp);
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
        _slots.SetLock(uid, HardsuitUpgradeRules.Slot, false);
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
        // One atomic multi-material transaction: partial payments are impossible.
        if (!_materials.TryChangeMaterialAmount((uid, null), cost.ToDictionary(p => p.Key, p => -p.Value), localOnly: true))
            return false;
        comp.Job = kind;
        comp.JobSuit = suit;
        comp.Target = target;
        comp.Duration = comp.Remaining = HardsuitUpgradeRules.Seconds(kind, target);
        comp.PaidMaterials = cost;
        _slots.SetLock(uid, HardsuitUpgradeRules.Slot, true);
        UpdateUi(uid, comp);
        return true;
    }

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
                if (Suit(uid) != comp.JobSuit || comp.JobSuit is not { } suit || Deleted(suit))
                    CancelAndRefund(uid, comp);
                else if (Powered(uid))
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
        var stored = _materials.GetStoredMaterials((uid, null), localOnly: true);
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
        _ui.SetUiState(uid, HardsuitUpgradeUiKey.Key, new HardsuitUpgradeState(suit is { } s ? Name(s) : null,
            Powered(uid), comp.Job != null, comp.Remaining, comp.Duration, options, quantities));
    }
}
