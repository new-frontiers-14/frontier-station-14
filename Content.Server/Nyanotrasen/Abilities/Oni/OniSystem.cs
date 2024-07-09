using Content.Server.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;

namespace Content.Server.Abilities.Oni
{
    public sealed class OniSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly SharedGunSystem _gunSystem = default!;

        private const double GunInaccuracyFactor = 17.0; // Frontier (20x<18x -> 10% buff)

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
                // Frontier: adjust penalty for wielded malus (ensuring it's actually wieldable)
                if (TryComp<GunWieldBonusComponent>(args.Entity, out var bonus) && HasComp<WieldableComponent>(args.Entity))
                {
                    //GunWieldBonus values are stored as negative.
                    heldComp.minAngleAdded = (gun.MinAngle + bonus.MinAngle) * GunInaccuracyFactor;
                    heldComp.angleIncreaseAdded = (gun.AngleIncrease + bonus.AngleIncrease) * GunInaccuracyFactor;
                    heldComp.maxAngleAdded = (gun.MaxAngle + bonus.MaxAngle) * GunInaccuracyFactor;
                }
                else
                {
                    heldComp.minAngleAdded = gun.MinAngle * GunInaccuracyFactor;
                    heldComp.angleIncreaseAdded = gun.AngleIncrease * GunInaccuracyFactor;
                    heldComp.maxAngleAdded = gun.MaxAngle * GunInaccuracyFactor;
                }

                gun.MinAngle += heldComp.minAngleAdded;
                gun.AngleIncrease += heldComp.angleIncreaseAdded;
                gun.MaxAngle += heldComp.maxAngleAdded;
                _gunSystem.RefreshModifiers(args.Entity); // Make sure values propagate to modified values (this also dirties the gun for us)
                // End Frontier
            }
        }

        private void OnEntRemoved(EntityUid uid, OniComponent component, EntRemovedFromContainerMessage args)
        {
            // Frontier: angle manipulation stored in HeldByOniComponent
            if (TryComp<GunComponent>(args.Entity, out var gun) &&
                TryComp<HeldByOniComponent>(args.Entity, out var heldComp))
            {
                gun.MinAngle -= heldComp.minAngleAdded;
                gun.AngleIncrease -= heldComp.angleIncreaseAdded;
                gun.MaxAngle -= heldComp.maxAngleAdded;
                _gunSystem.RefreshModifiers(args.Entity); // Make sure values propagate to modified values (this also dirties the gun for us)
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
