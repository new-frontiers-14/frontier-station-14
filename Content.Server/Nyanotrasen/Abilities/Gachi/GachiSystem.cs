using System.Linq;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.Events;
using Content.Server.Abilities.Gachi.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Abilities.Gachi
{
    public sealed class GachiSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GachiComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<GachiComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<GachiComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<JabroniOutfitComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<JabroniOutfitComponent, GotUnequippedEvent>(OnUnequipped);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var gachi in EntityQuery<GachiComponent>())
            {
                gachi.Accumulator += frameTime;
                if (gachi.Accumulator < gachi.AddToMultiplierTime.TotalSeconds)
                    continue;
                gachi.Accumulator -= (float) gachi.AddToMultiplierTime.TotalSeconds;
                if (gachi.Multiplier < 1f)
                    gachi.Multiplier += 0.01f;
            }
        }

        private void OnDamageChanged(EntityUid uid, GachiComponent component, DamageChangedEvent args)
        {
            if (TryComp<DamageableComponent>(uid, out var damageableComponent) && (damageableComponent.TotalDamage + args.DamageDelta?.Total >= 100))
                return;
            if (args.DamageIncreased && args.DamageDelta != null && args.DamageDelta.Total >= 5 && _random.Prob(0.3f * component.Multiplier))
            {
                FixedPoint2 newMultiplier = component.Multiplier - 0.25;
                component.Multiplier = (float) FixedPoint2.Max(FixedPoint2.Zero, newMultiplier);

                if (_random.Prob(0.01f))
                {
                    SoundSystem.Play( "/Audio/Effects/Gachi/ripears.ogg", Filter.Pvs(uid), AudioParams.Default.WithVolume(8f));
                    return;
                }
                SoundSystem.Play(component.PainSound.GetSound(), Filter.Pvs(uid), uid);

            }
        }

        private void OnMeleeHit(EntityUid uid, GachiComponent component, MeleeHitEvent args)
        {
            if (!args.IsHit ||
                !args.HitEntities.Any())
            {
                return;
            }

            if (_random.Prob(0.2f * component.Multiplier))
            {
                FixedPoint2 newMultiplier = component.Multiplier - 0.25;
                component.Multiplier = (float) FixedPoint2.Max(FixedPoint2.Zero, newMultiplier);
                SoundSystem.Play(component.HitOtherSound.GetSound(), Filter.Pvs(uid), uid);
            }
        }

        private void OnMobStateChanged(EntityUid uid, GachiComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == Shared.Mobs.MobState.Critical)
            {
                SoundSystem.Play("/Audio/Effects/Gachi/knockedhimout.ogg", Filter.Pvs(uid), uid);
            }
        }

        private void OnEquipped(EntityUid uid, JabroniOutfitComponent component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;
            EnsureComp<GachiComponent>(args.Equipee);
            component.IsActive = true;
        }

        private void OnUnequipped(EntityUid uid, JabroniOutfitComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;
            component.IsActive = false;
            RemComp<GachiComponent>(uid);
        }
    }
}
