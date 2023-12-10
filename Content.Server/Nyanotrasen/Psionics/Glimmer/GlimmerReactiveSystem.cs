using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Electrocution;
using Content.Server.Lightning;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Construction;
using Content.Server.Ghost;
using Content.Server.Revenant.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.GameTicking;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Verbs;
using Content.Shared.StatusEffect;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Construction.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Server.Psionics.Glimmer
{
    public sealed class GlimmerReactiveSystem : EntitySystem
    {
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
        [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _sharedAmbientSoundSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly LightningSystem _lightning = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly AnchorableSystem _anchorableSystem = default!;
        [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;
        [Dependency] private readonly GhostSystem _ghostSystem = default!;
        [Dependency] private readonly RevenantSystem _revenantSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        public float Accumulator = 0;
        public const float UpdateFrequency = 15f;
        public float BeamCooldown = 3;
        public GlimmerTier LastGlimmerTier = GlimmerTier.Minimal;
        public bool GhostsVisible = false;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

            SubscribeLocalEvent<SharedGlimmerReactiveComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, GlimmerTierChangedEvent>(OnTierChanged);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, GetVerbsEvent<AlternativeVerb>>(AddShockVerb);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, DestructionEventArgs>(OnDestroyed);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        }

        /// <summary>
        /// Update relevant state on an Entity.
        /// </summary>
        /// <param name="glimmerTierDelta">The number of steps in tier
        /// difference since last update. This can be zero for the sake of
        /// toggling the enabled states.</param>
        private void UpdateEntityState(EntityUid uid, SharedGlimmerReactiveComponent component, GlimmerTier currentGlimmerTier, int glimmerTierDelta)
        {
            var isEnabled = true;

            if (component.RequiresApcPower)
                if (TryComp(uid, out ApcPowerReceiverComponent? apcPower))
                    isEnabled = apcPower.Powered;

            _appearanceSystem.SetData(uid, GlimmerReactiveVisuals.GlimmerTier, isEnabled ? currentGlimmerTier : GlimmerTier.Minimal);

            // update ambient sound
            //if (TryComp(uid, out GlimmerSoundComponent? glimmerSound)
            //    && TryComp(uid, out AmbientSoundComponent? ambientSoundComponent)
            //    && glimmerSound.GetSound(currentGlimmerTier, out SoundSpecifier? spec))
            //{
            //    if (spec != null)
            //       _sharedAmbientSoundSystem.SetSound(uid, spec, ambientSoundComponent);
            //}

            if (component.ModulatesPointLight)
                if (TryComp(uid, out SharedPointLightComponent? pointLight))
                {
                    pointLight.Enabled = isEnabled ? currentGlimmerTier != GlimmerTier.Minimal : false;

                    // The light energy and radius are kept updated even when off
                    // to prevent the need to store additional state.
                    //
                    // Note that this doesn't handle edge cases where the
                    // PointLightComponent is removed while the
                    // GlimmerReactiveComponent is still present.
                    pointLight.Energy += glimmerTierDelta * component.GlimmerToLightEnergyFactor;
                    pointLight.Radius += glimmerTierDelta * component.GlimmerToLightRadiusFactor;
                }

        }

        /// <summary>
        /// Track when the component comes online so it can be given the
        /// current status of the glimmer tier, if it wasn't around when an
        /// update went out.
        /// </summary>
        private void OnMapInit(EntityUid uid, SharedGlimmerReactiveComponent component, MapInitEvent args)
        {
            if (component.RequiresApcPower && !HasComp<ApcPowerReceiverComponent>(uid))
                Logger.Warning($"{ToPrettyString(uid)} had RequiresApcPower set to true but no ApcPowerReceiverComponent was found on init.");

            if (component.ModulatesPointLight && !HasComp<SharedPointLightComponent>(uid))
                Logger.Warning($"{ToPrettyString(uid)} had ModulatesPointLight set to true but no PointLightComponent was found on init.");

            UpdateEntityState(uid, component, LastGlimmerTier, (int) LastGlimmerTier);
        }

        /// <summary>
        /// Reset the glimmer tier appearance data if the component's removed,
        /// just in case some objects can temporarily become reactive to the
        /// glimmer.
        /// </summary>
        private void OnComponentRemove(EntityUid uid, SharedGlimmerReactiveComponent component, ComponentRemove args)
        {
            UpdateEntityState(uid, component, GlimmerTier.Minimal, -1 * (int) LastGlimmerTier);
        }

        /// <summary>
        /// If the Entity has RequiresApcPower set to true, this will force an
        /// update to the entity's state.
        /// </summary>
        private void OnPowerChanged(EntityUid uid, SharedGlimmerReactiveComponent component, ref PowerChangedEvent args)
        {
            if (component.RequiresApcPower)
                UpdateEntityState(uid, component, LastGlimmerTier, 0);
        }

        /// <summary>
        ///     Enable / disable special effects from higher tiers.
        /// </summary>
        private void OnTierChanged(EntityUid uid, SharedGlimmerReactiveComponent component, GlimmerTierChangedEvent args)
        {
            if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
                return;

            if (args.CurrentTier >= GlimmerTier.Dangerous)
            {
                if (!Transform(uid).Anchored)
                    AnchorOrExplode(uid);

                receiver.PowerDisabled = false;
                receiver.NeedsPower = false;
            } else
            {
                receiver.NeedsPower = true;
            }
        }

        private void AddShockVerb(EntityUid uid, SharedGlimmerReactiveComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if(!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
                return;

            if (receiver.NeedsPower)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    _sharedAudioSystem.PlayPvs(component.ShockNoises, args.User);
                    _electrocutionSystem.TryDoElectrocution(args.User, null, _glimmerSystem.Glimmer / 200, TimeSpan.FromSeconds((float) _glimmerSystem.Glimmer / 100), false);
                },
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
                Text = Loc.GetString("power-switch-component-toggle-verb"),
                Priority = -3
            };
            args.Verbs.Add(verb);
        }

        private void OnDamageChanged(EntityUid uid, SharedGlimmerReactiveComponent component, DamageChangedEvent args)
        {
            if (args.Origin == null)
                return;

            if (!_random.Prob((float) _glimmerSystem.Glimmer / 1000))
                return;

            var tier = _glimmerSystem.GetGlimmerTier();
            if (tier < GlimmerTier.High)
                return;
            Beam(uid, args.Origin.Value, tier);
        }

        private void OnDestroyed(EntityUid uid, SharedGlimmerReactiveComponent component, DestructionEventArgs args)
        {
            Spawn("MaterialBluespace1", Transform(uid).Coordinates);

            var tier = _glimmerSystem.GetGlimmerTier();
            if (tier < GlimmerTier.High)
                return;

            var totalIntensity = (float) (_glimmerSystem.Glimmer * 2);
            var slope = (float) (11 - _glimmerSystem.Glimmer / 100);
            var maxIntensity = 20;

            var removed = (float) _glimmerSystem.Glimmer * _random.NextFloat(0.1f, 0.15f);
            _glimmerSystem.Glimmer -= (int) removed;
            BeamRandomNearProber(uid, _glimmerSystem.Glimmer / 350, _glimmerSystem.Glimmer / 50);
            _explosionSystem.QueueExplosion(uid, "Default", totalIntensity, slope, maxIntensity);
        }

        private void OnUnanchorAttempt(EntityUid uid, SharedGlimmerReactiveComponent component, UnanchorAttemptEvent args)
        {
            if (_glimmerSystem.GetGlimmerTier() >= GlimmerTier.Dangerous)
            {
                _sharedAudioSystem.PlayPvs(component.ShockNoises, args.User);
                _electrocutionSystem.TryDoElectrocution(args.User, null, _glimmerSystem.Glimmer / 200, TimeSpan.FromSeconds((float) _glimmerSystem.Glimmer / 100), false);
                args.Cancel();
            }
        }

        public void BeamRandomNearProber(EntityUid prober, int targets, float range = 10f)
        {
            List<EntityUid> targetList = new();
            foreach (var target in _entityLookupSystem.GetComponentsInRange<StatusEffectsComponent>(Transform(prober).Coordinates, range))
            {
                if (target.AllowedEffects.Contains("Electrocution"))
                    targetList.Add(target.Owner);
            }

            foreach(var reactive in _entityLookupSystem.GetComponentsInRange<SharedGlimmerReactiveComponent>(Transform(prober).Coordinates, range))
            {
                targetList.Add(reactive.Owner);
            }

            _random.Shuffle(targetList);
            foreach (var target in targetList)
            {
                if (targets <= 0)
                    return;

                Beam(prober, target, _glimmerSystem.GetGlimmerTier(), false);
                targets--;
            }
        }

        private void Beam(EntityUid prober, EntityUid target, GlimmerTier tier, bool obeyCD = true)
        {
            if (obeyCD && BeamCooldown != 0)
                return;

            if (Deleted(prober) || Deleted(target))
                return;

            var lxform = Transform(prober);
            var txform = Transform(target);

            if (!lxform.Coordinates.TryDistance(EntityManager, txform.Coordinates, out var distance))
                return;
            if (distance > (float) (_glimmerSystem.Glimmer / 100))
                return;

            string beamproto;

            switch (tier)
            {
                case GlimmerTier.Dangerous:
                    beamproto = "SuperchargedLightning";
                    break;
                case GlimmerTier.Critical:
                    beamproto = "HyperchargedLightning";
                    break;
                default:
                    beamproto = "ChargedLightning";
                    break;
            }


            _lightning.ShootLightning(prober, target, beamproto);
            BeamCooldown += 3f;
        }

        private void AnchorOrExplode(EntityUid uid)
        {
            var xform = Transform(uid);
            if (xform.Anchored)
                return;

            if (!TryComp<PhysicsComponent>(uid, out var physics))
                return;

            var coordinates = xform.Coordinates;
            var gridUid = xform.GridUid;

            if (_mapManager.TryGetGrid(gridUid, out var grid))
            {
                var tileIndices = grid.TileIndicesFor(coordinates);

                if (_anchorableSystem.TileFree(grid, tileIndices, physics.CollisionLayer, physics.CollisionMask) &&
                    _transformSystem.AnchorEntity(uid, xform))
                {
                    return;
                }
            }

            // Wasn't able to get a grid or a free tile, so explode.
            _destructibleSystem.DestroyEntity(uid);
        }

        private void Reset(RoundRestartCleanupEvent args)
        {
            Accumulator = 0;

            // It is necessary that the GlimmerTier is reset to the default
            // tier on round restart. This system will persist through
            // restarts, and an undesired event will fire as a result after the
            // start of the new round, causing modulatable PointLights to have
            // negative Energy if the tier was higher than Minimal on restart.
            LastGlimmerTier = GlimmerTier.Minimal;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            Accumulator += frameTime;
            BeamCooldown = Math.Max(0, BeamCooldown - frameTime);

            if (Accumulator > UpdateFrequency)
            {
                var currentGlimmerTier = _glimmerSystem.GetGlimmerTier();

                var reactives = EntityQuery<SharedGlimmerReactiveComponent>();
                if (currentGlimmerTier != LastGlimmerTier) {
                    var glimmerTierDelta = (int) currentGlimmerTier - (int) LastGlimmerTier;
                    var ev = new GlimmerTierChangedEvent(LastGlimmerTier, currentGlimmerTier, glimmerTierDelta);

                    foreach (var reactive in reactives)
                    {
                        UpdateEntityState(reactive.Owner, reactive, currentGlimmerTier, glimmerTierDelta);
                        RaiseLocalEvent(reactive.Owner, ev);
                    }

                    LastGlimmerTier = currentGlimmerTier;
                }
                if (currentGlimmerTier == GlimmerTier.Critical)
                {
                    _ghostSystem.MakeVisible(true);
                    _revenantSystem.MakeVisible(true);
                    GhostsVisible = true;
                    foreach (var reactive in reactives)
                    {
                        BeamRandomNearProber(reactive.Owner, 1, 12);
                    }
                } else if (GhostsVisible == true)
                {
                    _ghostSystem.MakeVisible(false);
                    _revenantSystem.MakeVisible(false);
                    GhostsVisible = false;
                }
                Accumulator = 0;
            }
        }
    }

    /// <summary>
    /// This event is fired when the broader glimmer tier has changed,
    /// not on every single adjustment to the glimmer count.
    ///
    /// <see cref="GlimmerSystem.GetGlimmerTier"/> has the exact
    /// values corresponding to tiers.
    /// </summary>
    public class GlimmerTierChangedEvent : EntityEventArgs
    {
        /// <summary>
        /// What was the last glimmer tier before this event fired?
        /// </summary>
        public readonly GlimmerTier LastTier;

        /// <summary>
        /// What is the current glimmer tier?
        /// </summary>
        public readonly GlimmerTier CurrentTier;

        /// <summary>
        /// What is the change in tiers between the last and current tier?
        /// </summary>
        public readonly int TierDelta;

        public GlimmerTierChangedEvent(GlimmerTier lastTier, GlimmerTier currentTier, int tierDelta)
        {
            LastTier = lastTier;
            CurrentTier = currentTier;
            TierDelta = tierDelta;
        }
    }
}

