using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Explosion.Components;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Payload.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Server.Station.Systems;

namespace Content.Server.Explosion.EntitySystems
{
    /// <summary>
    /// Raised whenever something is Triggered on the entity.
    /// </summary>
    public sealed class TriggerEvent : HandledEntityEventArgs
    {
        public EntityUid Triggered { get; }
        public EntityUid? User { get; }

        public TriggerEvent(EntityUid triggered, EntityUid? user = null)
        {
            Triggered = triggered;
            User = user;
        }
    }

    /// <summary>
    /// Raised when timer trigger becomes active.
    /// </summary>
    [ByRefEvent]
    public readonly record struct ActiveTimerTriggerEvent(EntityUid Triggered, EntityUid? User);

    [UsedImplicitly]
    public sealed partial class TriggerSystem : EntitySystem
    {
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly FlashSystem _flashSystem = default!;
        [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StationSystem _station = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeProximity();
            InitializeOnUse();
            InitializeSignal();
            InitializeTimedCollide();
            InitializeVoice();
            InitializeMobstate();

            SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnTriggerCollide);
            SubscribeLocalEvent<TriggerOnActivateComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<TriggerImplantActionComponent, ActivateImplantEvent>(OnImplantTrigger);
            SubscribeLocalEvent<TriggerOnStepTriggerComponent, StepTriggeredEvent>(OnStepTriggered);
            SubscribeLocalEvent<TriggerOnSlipComponent, SlipEvent>(OnSlipTriggered);
            SubscribeLocalEvent<TriggerWhenEmptyComponent, OnEmptyGunShotEvent>(OnEmptyTriggered);

            SubscribeLocalEvent<SpawnOnTriggerComponent, TriggerEvent>(OnSpawnTrigger);
            SubscribeLocalEvent<DeleteOnTriggerComponent, TriggerEvent>(HandleDeleteTrigger);
            SubscribeLocalEvent<ExplodeOnTriggerComponent, TriggerEvent>(HandleExplodeTrigger);
            SubscribeLocalEvent<FlashOnTriggerComponent, TriggerEvent>(HandleFlashTrigger);
            SubscribeLocalEvent<GibOnTriggerComponent, TriggerEvent>(HandleGibTrigger);

            SubscribeLocalEvent<AnchorOnTriggerComponent, TriggerEvent>(OnAnchorTrigger);
            SubscribeLocalEvent<SoundOnTriggerComponent, TriggerEvent>(OnSoundTrigger);
            SubscribeLocalEvent<RattleComponent, TriggerEvent>(HandleRattleTrigger);
        }

        private void OnSoundTrigger(EntityUid uid, SoundOnTriggerComponent component, TriggerEvent args)
        {
            _audio.PlayPvs(component.Sound, uid);
            if (component.RemoveOnTrigger)
                RemCompDeferred<SoundOnTriggerComponent>(uid);
        }

        private void OnAnchorTrigger(EntityUid uid, AnchorOnTriggerComponent component, TriggerEvent args)
        {
            var xform = Transform(uid);

            if (xform.Anchored)
                return;

            _transformSystem.AnchorEntity(uid, xform);

            if (component.RemoveOnTrigger)
                RemCompDeferred<AnchorOnTriggerComponent>(uid);
        }

        private void OnSpawnTrigger(EntityUid uid, SpawnOnTriggerComponent component, TriggerEvent args)
        {
            var xform = Transform(uid);

            var coords = xform.Coordinates;

            if (!coords.IsValid(EntityManager))
                return;

            Spawn(component.Proto, coords);
        }

        private void HandleExplodeTrigger(EntityUid uid, ExplodeOnTriggerComponent component, TriggerEvent args)
        {
            _explosions.TriggerExplosive(uid, user: args.User);
            args.Handled = true;
        }

        private void HandleFlashTrigger(EntityUid uid, FlashOnTriggerComponent component, TriggerEvent args)
        {
            // TODO Make flash durations sane ffs.
            _flashSystem.FlashArea(uid, args.User, component.Range, component.Duration * 1000f);
            args.Handled = true;
        }

        private void HandleDeleteTrigger(EntityUid uid, DeleteOnTriggerComponent component, TriggerEvent args)
        {
            EntityManager.QueueDeleteEntity(uid);
            args.Handled = true;
        }

        private void HandleGibTrigger(EntityUid uid, GibOnTriggerComponent component, TriggerEvent args)
        {
            if (!TryComp<TransformComponent>(uid, out var xform))
                return;

            _body.GibBody(xform.ParentUid, deleteItems: component.DeleteItems);

            args.Handled = true;
        }

        private void HandleRattleTrigger(EntityUid uid, RattleComponent component, TriggerEvent args)
        {
            if (!TryComp<SubdermalImplantComponent?>(uid, out var implanted))
                return;

            if (implanted.ImplantedEntity == null)
                return;

            // Gets location of the implant
            var ownerXform = Transform(uid);
            var pos = ownerXform.MapPosition;
            var x = (int) pos.X;
            var y = (int) pos.Y;
            var posText = $"({x}, {y})";

            // Gets station location of the implant
            var station = _station.GetOwningStation(uid);
            var stationName = station is null ? null : Name(station.Value);

            if (!(stationName == null))
            {
                stationName += " ";
            }
            else
            {
                stationName = "";
            }

            var critMessage = Loc.GetString(component.CritMessage, ("user", implanted.ImplantedEntity.Value), ("grid", stationName!), ("position", posText));
            var deathMessage = Loc.GetString(component.DeathMessage, ("user", implanted.ImplantedEntity.Value), ("grid", stationName!), ("position", posText));

            if (!TryComp<MobStateComponent>(implanted.ImplantedEntity, out var mobstate))
                return;

            // Sends a message to the radio channel specified by the implant
            if (mobstate.CurrentState == MobState.Critical)
                _radioSystem.SendRadioMessage(uid, critMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);
            if (mobstate.CurrentState == MobState.Dead)
                _radioSystem.SendRadioMessage(uid, deathMessage, _prototypeManager.Index<RadioChannelPrototype>(component.RadioChannel), uid);

            args.Handled = true;
        }

        private void OnTriggerCollide(EntityUid uid, TriggerOnCollideComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixtureId == component.FixtureID && (!component.IgnoreOtherNonHard || args.OtherFixture.Hard))
                Trigger(uid);
        }

        private void OnActivate(EntityUid uid, TriggerOnActivateComponent component, ActivateInWorldEvent args)
        {
            Trigger(uid, args.User);
            args.Handled = true;
        }

        private void OnImplantTrigger(EntityUid uid, TriggerImplantActionComponent component, ActivateImplantEvent args)
        {
            args.Handled = Trigger(uid);
        }

        private void OnStepTriggered(EntityUid uid, TriggerOnStepTriggerComponent component, ref StepTriggeredEvent args)
        {
            Trigger(uid, args.Tripper);
        }

        private void OnSlipTriggered(EntityUid uid, TriggerOnSlipComponent component, ref SlipEvent args)
        {
            Trigger(uid, args.Slipped);
        }

        private void OnEmptyTriggered(EntityUid uid, TriggerWhenEmptyComponent component, ref OnEmptyGunShotEvent args)
        {
            Trigger(uid, args.EmptyGun);
        }

        public bool Trigger(EntityUid trigger, EntityUid? user = null)
        {
            var triggerEvent = new TriggerEvent(trigger, user);
            EntityManager.EventBus.RaiseLocalEvent(trigger, triggerEvent, true);
            return triggerEvent.Handled;
        }

        public void TryDelay(EntityUid uid, float amount, ActiveTimerTriggerComponent? comp = null)
        {
            if (!Resolve(uid, ref comp, false))
                return;

            comp.TimeRemaining += amount;
        }

        public void HandleTimerTrigger(EntityUid uid, EntityUid? user, float delay , float beepInterval, float? initialBeepDelay, SoundSpecifier? beepSound)
        {
            if (delay <= 0)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);
                Trigger(uid, user);
                return;
            }

            if (HasComp<ActiveTimerTriggerComponent>(uid))
                return;

            if (user != null)
            {
                // Check if entity is bomb/mod. grenade/etc
                if (_container.TryGetContainer(uid, "payload", out BaseContainer? container) &&
                    container.ContainedEntities.Count > 0 &&
                    TryComp(container.ContainedEntities[0], out ChemicalPayloadComponent? chemicalPayloadComponent))
                {
                    // If a beaker is missing, the entity won't explode, so no reason to log it
                    if (!TryComp(chemicalPayloadComponent?.BeakerSlotA.Item, out SolutionContainerManagerComponent? beakerA) ||
                        !TryComp(chemicalPayloadComponent?.BeakerSlotB.Item, out SolutionContainerManagerComponent? beakerB))
                        return;

                    _adminLogger.Add(LogType.Trigger,
                        $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}, which contains [{string.Join(", ", beakerA.Solutions.Values.First())}] in one beaker and [{string.Join(", ", beakerB.Solutions.Values.First())}] in the other.");
                }
                else
                {
                    _adminLogger.Add(LogType.Trigger,
                        $"{ToPrettyString(user.Value):user} started a {delay} second timer trigger on entity {ToPrettyString(uid):timer}");
                }

            }
            else
            {
                _adminLogger.Add(LogType.Trigger,
                    $"{delay} second timer trigger started on entity {ToPrettyString(uid):timer}");
            }

            var active = AddComp<ActiveTimerTriggerComponent>(uid);
            active.TimeRemaining = delay;
            active.User = user;
            active.BeepSound = beepSound;
            active.BeepInterval = beepInterval;
            active.TimeUntilBeep = initialBeepDelay == null ? active.BeepInterval : initialBeepDelay.Value;

            var ev = new ActiveTimerTriggerEvent(uid, user);
            RaiseLocalEvent(uid, ref ev);

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Primed, appearance);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdateProximity();
            UpdateTimer(frameTime);
            UpdateTimedCollide(frameTime);
        }

        private void UpdateTimer(float frameTime)
        {
            HashSet<EntityUid> toRemove = new();
            var query = EntityQueryEnumerator<ActiveTimerTriggerComponent>();
            while (query.MoveNext(out var uid, out var timer))
            {
                timer.TimeRemaining -= frameTime;
                timer.TimeUntilBeep -= frameTime;

                if (timer.TimeRemaining <= 0)
                {
                    Trigger(uid, timer.User);
                    toRemove.Add(uid);
                    continue;
                }

                if (timer.BeepSound == null || timer.TimeUntilBeep > 0)
                    continue;

                timer.TimeUntilBeep += timer.BeepInterval;
                _audio.PlayPvs(timer.BeepSound, uid, timer.BeepSound.Params);
            }

            foreach (var uid in toRemove)
            {
                RemComp<ActiveTimerTriggerComponent>(uid);

                // In case this is a re-usable grenade, un-prime it.
                if (TryComp<AppearanceComponent>(uid, out var appearance))
                    _appearance.SetData(uid, TriggerVisuals.VisualState, TriggerVisualState.Unprimed, appearance);
            }
        }
    }
}
