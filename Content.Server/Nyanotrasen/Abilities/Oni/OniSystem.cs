using Content.Server.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Content.Shared.Wieldable.Components;

namespace Content.Server.Abilities.Oni
{
    public sealed class OniSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OniComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<OniComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<OniComponent, MeleeHitEvent>(OnOniMeleeHit);
            SubscribeLocalEvent<HeldByOniComponent, MeleeHitEvent>(OnHeldMeleeHit);
            SubscribeLocalEvent<HeldByOniComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnEntInserted(EntityUid uid, OniComponent component, EntInsertedIntoContainerMessage args)
        {
            var heldComp = EnsureComp<HeldByOniComponent>(args.Entity);
            heldComp.Holder = uid;

            if (TryComp<GunComponent>(args.Entity, out var gun))
            {
                // Frontier: adjust penalty for wielded malus
                if (TryComp<GunWieldBonusComponent>(args.Entity, out var bonus))
                {
                    //GunWieldBonus values are stored as negative.
                    heldComp.minAngleAdded = (gun.MinAngle + bonus.MinAngle) * 19.0;
                    heldComp.angleIncreaseAdded = (gun.AngleIncrease + bonus.AngleIncrease) * 19.0;
                    heldComp.maxAngleAdded = (gun.MaxAngle + bonus.MaxAngle) * 19.0;
                }
                else
                {
                    heldComp.minAngleAdded = gun.MinAngle * 19.0;
                    heldComp.angleIncreaseAdded = gun.AngleIncrease * 19.0;
                    heldComp.maxAngleAdded = gun.MaxAngle * 19.0;
                }
                gun.MinAngle += heldComp.minAngleAdded;
                gun.AngleIncrease += heldComp.angleIncreaseAdded;
                gun.MaxAngle += heldComp.maxAngleAdded;
                // End Frontier
            }
        }

        private void OnEntRemoved(EntityUid uid, OniComponent component, EntRemovedFromContainerMessage args)
        {
            // Frontier: store angle manipulation in HeldByOniComponent
            if (TryComp<GunComponent>(args.Entity, out var gun) &&
                TryComp<HeldByOniComponent>(args.Entity, out var heldByOni))
            {
                gun.MinAngle -= heldByOni.minAngleAdded;
                gun.AngleIncrease -= heldByOni.angleIncreaseAdded;
                gun.MaxAngle -= heldByOni.maxAngleAdded;
            }
            // End Frontier

            RemComp<HeldByOniComponent>(args.Entity);
        }

        private void OnOniMeleeHit(EntityUid uid, OniComponent component, MeleeHitEvent args)
        {
            args.ModifiersList.Add(component.MeleeModifiers);
        }

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
