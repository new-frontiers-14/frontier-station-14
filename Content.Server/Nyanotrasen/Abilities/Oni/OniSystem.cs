using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._NF.Weapons.Components;
using Robust.Shared.Containers;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;

namespace Content.Server.Abilities.Oni
{
    public sealed class OniSystem : EntitySystem
    {
        [Dependency] private readonly SharedGunSystem _gunSystem = default!;

        private const double GunInaccuracyFactor = 17.0; // Frontier (20x<18x -> 10% buff)

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OniComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<OniComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<OniComponent, MeleeHitEvent>(OnOniMeleeHit);
            // Frontier: Modify gun in event
            SubscribeLocalEvent<HeldByOniComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers,
                after: [typeof(SharedWieldableSystem)]);
            // End Frontier
            SubscribeLocalEvent<HeldByOniComponent, MeleeHitEvent>(OnHeldMeleeHit);
            SubscribeLocalEvent<HeldByOniComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnEntInserted(EntityUid uid, OniComponent component, EntInsertedIntoContainerMessage args)
        {
            var heldComp = EnsureComp<HeldByOniComponent>(args.Entity);
            heldComp.Holder = uid;

            // Frontier: Oni-friendly "guns" (crusher)
            if (TryComp<GunComponent>(args.Entity, out var gun) && !HasComp<NFOniFriendlyGunComponent>(args.Entity))
            {
                _gunSystem.RefreshModifiers(args.Entity);
            }
            // End Frontier
        }

        private void OnEntRemoved(EntityUid uid, OniComponent component, EntRemovedFromContainerMessage args)
        {
            RemComp<HeldByOniComponent>(args.Entity);

            // Frontier: angle manipulation
            // Frontier: Oni-friendly "guns" (crusher)
            if (HasComp<GunComponent>(args.Entity) && !HasComp<NFOniFriendlyGunComponent>(args.Entity))
            {
                _gunSystem.RefreshModifiers(args.Entity);
            }
            // End Frontier
        }

        private void OnOniMeleeHit(EntityUid uid, OniComponent component, MeleeHitEvent args)
        {
            args.ModifiersList.Add(component.MeleeModifiers);
        }

        // Frontier: Modify gun in event
        private void OnGunRefreshModifiers(Entity<HeldByOniComponent> ent, ref GunRefreshModifiersEvent args)
        {
            // Frontier: Oni-friendly "guns" (crusher)
            if (!HasComp<NFOniFriendlyGunComponent>(args.Gun))
            {
                args.MinAngle *= GunInaccuracyFactor;
                args.AngleIncrease *= GunInaccuracyFactor;
                args.MaxAngle *= GunInaccuracyFactor;
            }
        }
        // End Frontier

        private void OnHeldMeleeHit(EntityUid uid, HeldByOniComponent component, MeleeHitEvent args)
        {
            if (!TryComp<OniComponent>(component.Holder, out var oni))
                return;

            args.ModifiersList.Add(oni.MeleeModifiers);
        }

        private void OnStamHit(EntityUid uid, HeldByOniComponent component, StaminaMeleeHitEvent args)
        {
            if (!TryComp<OniComponent>(component.Holder, out var oni))
                return;

            args.Multiplier *= oni.StamDamageMultiplier;
        }
    }
}
