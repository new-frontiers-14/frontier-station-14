using Content.Shared.Abilities.Psionics;
using Content.Shared.StatusEffect;
using Content.Shared.Mobs;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Damage.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.CCVar;
using Content.Server.Abilities.Psionics;
using Content.Server.Chat.Systems;
using Content.Server.Electrocution;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.Psionics
{
    public sealed class PsionicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
        [Dependency] private readonly MindSwapPowerSystem _mindSwapPowerSystem = default!;
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly NpcFactionSystem _npcFactonSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        /// <summary>
        /// Unfortunately, since spawning as a normal role and anything else is so different,
        /// this is the only way to unify them, for now at least.
        /// </summary>
        Queue<(PotentialPsionicComponent component, EntityUid uid)> _rollers = new();
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var roller in _rollers)
            {
                RollPsionics(roller.uid, roller.component, false);
            }
            _rollers.Clear();
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PotentialPsionicComponent, MapInitEvent>(OnStartup);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, StaminaMeleeHitEvent>(OnStamHit);

            SubscribeLocalEvent<PotentialPsionicComponent, MobStateChangedEvent>(OnDeathGasp);

            SubscribeLocalEvent<PsionicComponent, ComponentInit>(OnInit);
            //SubscribeLocalEvent<PsionicComponent, ComponentRemove>(OnRemove);
        }

        private void OnStartup(EntityUid uid, PotentialPsionicComponent component, MapInitEvent args)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            _rollers.Enqueue((component, uid));
        }

        private void OnMeleeHit(EntityUid uid, AntiPsionicWeaponComponent component, MeleeHitEvent args)
        {
            foreach (var entity in args.HitEntities)
            {
                if (HasComp<PsionicComponent>(entity))
                {
                    SoundSystem.Play("/Audio/Effects/lightburn.ogg", Filter.Pvs(entity), entity);
                    args.ModifiersList.Add(component.Modifiers);
                    if (_random.Prob(component.DisableChance))
                        _statusEffects.TryAddStatusEffect(entity, "PsionicsDisabled", TimeSpan.FromSeconds(10), true, "PsionicsDisabled");
                }

                if (TryComp<MindSwappedComponent>(entity, out var swapped))
                {
                    _mindSwapPowerSystem.Swap(entity, swapped.OriginalEntity, true);
                    return;
                }

                if (component.Punish && HasComp<PotentialPsionicComponent>(entity) && !HasComp<PsionicComponent>(entity) && _random.Prob(0.5f))
                    _electrocutionSystem.TryDoElectrocution(args.User, null, 20, TimeSpan.FromSeconds(5), false);
            }
        }

        private void OnDeathGasp(EntityUid uid, PotentialPsionicComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState != MobState.Dead)
                return;

            string message;

            switch (_glimmerSystem.GetGlimmerTier())
            {
                case GlimmerTier.Critical:
                    message = Loc.GetString("death-gasp-high", ("ent", Identity.Entity(uid, EntityManager)));
                    break;
                case GlimmerTier.Dangerous:
                    message = Loc.GetString("death-gasp-medium", ("ent",Identity.Entity(uid, EntityManager)));
                    break;
                default:
                    message = Loc.GetString("death-gasp-normal", ("ent", Identity.Entity(uid, EntityManager)));
                    break;
            }

            _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Emote, false);
        }

        private void OnInit(EntityUid uid, PsionicComponent component, ComponentInit args)
        {
            if (!component.Removable)
                return;

            if (!TryComp<NpcFactionMemberComponent>(uid, out var factions))
                return;

            //if (_npcFactonSystem.ContainsFaction(uid, "GlimmerMonster", factions))
            //    return;

            //_npcFactonSystem.AddFaction(uid, "PsionicInterloper");
        }

        //private void OnRemove(EntityUid uid, PsionicComponent component, ComponentRemove args)
        //{
        //    if (!TryComp<NpcFactionMemberComponent>(uid, out var factions))
         //       return;
//
         //   _npcFactonSystem.RemoveFaction(uid, "PsionicInterloper");
        //}

        private void OnStamHit(EntityUid uid, AntiPsionicWeaponComponent component, StaminaMeleeHitEvent args)
        {
            var bonus = false;
            foreach (var stam in args.HitList)
            {
                if (HasComp<PsionicComponent>(stam.Entity))
                    bonus = true;
            }

            if (!bonus)
                return;


            args.FlatModifier += component.PsychicStaminaDamage;
        }

        public void RollPsionics(EntityUid uid, PotentialPsionicComponent component, bool applyGlimmer = true, float multiplier = 1f)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            if (!_cfg.GetCVar(CCVars.PsionicRollsEnabled))
                return;

            var chance = component.Chance;
            var warn = true;
            if (TryComp<PsionicBonusChanceComponent>(uid, out var bonus))
            {
                chance *= bonus.Multiplier;
                chance += bonus.FlatBonus;
                warn = bonus.Warn;
            }

            if (applyGlimmer)
                chance += ((float) _glimmerSystem.Glimmer / 1000);

            chance *= multiplier;

            chance = Math.Clamp(chance, 0, 1);

            if (_random.Prob(chance))
                _psionicAbilitiesSystem.AddPsionics(uid, warn);
        }

        public void RerollPsionics(EntityUid uid, PotentialPsionicComponent? psionic = null, float bonusMuliplier = 1f)
        {
            if (!Resolve(uid, ref psionic, false))
                return;

            if (psionic.Rerolled)
                return;

            RollPsionics(uid, psionic, multiplier: bonusMuliplier);
            psionic.Rerolled = true;
        }
    }
}
