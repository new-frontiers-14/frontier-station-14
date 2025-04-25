using Content.Server.Ghost;
using Content.Server._NF.Skrungler.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared._NF.Skrungler;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Content.Shared.Jittering;
using Content.Shared.Audio;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Construction;
using Content.Shared.Materials;
using Content.Server.Materials;
using Content.Shared.Power;
using Content.Shared.Construction.Components;
using Content.Server.Power.Components;
using Content.Server.Body.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server._NF.Skrungler;

public sealed class SkrunglerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;

    [ValidatePrototypeId<MaterialPrototype>]
    public const string FuelPrototype = "FuelGradePlasma";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveSkrunglerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ActiveSkrunglerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SkrunglerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ActiveSkrunglerComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<SkrunglerComponent, GetVerbsEvent<AlternativeVerb>>(AddskrungelVerb);
        SubscribeLocalEvent<SkrunglerComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<ActiveSkrunglerComponent, StorageOpenAttemptEvent>(OnAttemptOpen);
        SubscribeLocalEvent<SkrunglerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SkrunglerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SkrunglerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnExamine(EntityUid uid, SkrunglerComponent component, ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        using (args.PushGroup(nameof(SkrunglerComponent)))
        {
            if (_appearance.TryGetData<bool>(uid, SkrunglerVisuals.Skrungling, out var isSkrungling, appearance) &&
                isSkrungling)
            {
                args.PushMarkup(Loc.GetString("skrungler-entity-storage-component-on-examine-details-is-running",
                    ("owner", uid)));
            }

            if (_appearance.TryGetData<bool>(uid, StorageVisuals.HasContents, out var hasContents, appearance) &&
                hasContents)
            {
                args.PushMarkup(Loc.GetString("skrungler-entity-storage-component-on-examine-details-has-contents"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("skrungler-entity-storage-component-on-examine-details-empty"));
            }
        }
    }

    private void OnAttemptOpen(EntityUid uid, ActiveSkrunglerComponent component, ref StorageOpenAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void AddskrungelVerb(EntityUid uid, SkrunglerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || storage.Open)
            return;

        if (HasComp<ActiveSkrunglerComponent>(uid))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("skrungel-verb-get-data-text"),
            // TODO VERB ICON add flame/burn symbol?

            Act = () => TryProcessing(uid, component, storage),
            Impact = LogImpact.High // could be a body? or evidence? I dunno.
        };
        args.Verbs.Add(verb);
    }

    public bool TryProcessing(EntityUid uid, SkrunglerComponent component, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref storage))
            return false;

        if (storage.Open || storage.Contents.ContainedEntities.Count < 1)
            return false;

        // Refuse to accept alive mobs and dead-but-connected players
        var entity = storage.Contents.ContainedEntities[0];
        if (entity is not { Valid: true })
            return false;

        if (TryComp<MobStateComponent>(entity, out var comp) && !_mobState.IsDead(entity, comp))
            return false;

        if (_minds.TryGetMind(entity, out var _, out var mind) && mind.Session?.State?.Status == SessionStatus.InGame)
            return false;

        StartProcessing(entity, new Entity<SkrunglerComponent>(uid, component));
        return true;
    }

    private void StartProcessing(EntityUid toProcess, Entity<SkrunglerComponent> ent, PhysicsComponent? physics = null)
    {
        if (!Resolve(toProcess, ref physics))
            return;

        var component = ent.Comp;
        AddComp<ActiveSkrunglerComponent>(ent);

        if (TryComp<BloodstreamComponent>(toProcess, out var stream))
        {
            component.BloodReagent = stream.BloodReagent;
        }

        var expectedYield = physics.FixturesMass * component.YieldPerUnitMass;
        component.CurrentExpectedYield += expectedYield;

        component.ProcessingTimer = physics.FixturesMass * component.ProcessingTimePerUnitMass;

        QueueDel(toProcess);
    }

    private void OnSuicideByEnvironment(Entity<SkrunglerComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ActiveSkrunglerComponent>(ent))
            return;

        if (TryComp<ApcPowerReceiverComponent>(ent, out var power) && !power.Powered)
            return;

        if (TryComp(args.Victim, out ActorComponent? actor) && _minds.TryGetMind(args.Victim, out var mindId, out var mind))
        {
            _ghostSystem.OnGhostAttempt(mindId, false, mind: mind);

            if (mind.OwnedEntity is { Valid: true } entity)
            {
                _popup.PopupEntity(Loc.GetString("skrungler-entity-storage-component-suicide-message"), entity);
            }
        }

        _popup.PopupEntity(Loc.GetString("skrungler-entity-storage-component-suicide-message-others",
            ("victim", Identity.Entity(args.Victim, EntityManager))),
            args.Victim, Filter.PvsExcept(args.Victim), true, PopupType.LargeCaution);

        if (_entityStorage.CanInsert(args.Victim, ent))
        {
            _entityStorage.CloseStorage(ent);
            _standing.Down(args.Victim, false);
            _entityStorage.Insert(args.Victim, ent);
        }
        else
        {
            EntityManager.DeleteEntity(args.Victim);
        }

        _entityStorage.CloseStorage(ent);
        StartProcessing(args.Victim, ent);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSkrunglerComponent, SkrunglerComponent, EntityStorageComponent>();
        while (query.MoveNext(out var uid, out var act, out var skrug, out var storage))
        {
            skrug.ProcessingTimer -= frameTime;
            skrug.RandomMessTimer -= frameTime;

            if (skrug.RandomMessTimer <= 0)
            {
                if (_robustRandom.Prob(0.2f) && skrug.BloodReagent is not null)
                {
                    Solution blood = new();
                    blood.AddReagent(skrug.BloodReagent, 50);
                    _puddleSystem.TrySpillAt(uid, blood, out _);
                }
                skrug.RandomMessTimer += (float)skrug.RandomMessInterval.TotalSeconds;
            }

            if (skrug.ProcessingTimer > 0)
            {
                continue;
            }

            var actualYield = (int)(skrug.CurrentExpectedYield); // can only have integer
            skrug.CurrentExpectedYield = skrug.CurrentExpectedYield - actualYield; // store non-integer leftovers

            if (!Resolve(uid, ref storage))
                return;

            var fuel = _material.SpawnMultipleFromMaterial(actualYield, FuelPrototype, Transform(uid).Coordinates);
            foreach (var fuelEntity in fuel)
            {
                _containers.Insert(fuelEntity, storage.Contents);
            }

            skrug.BloodReagent = null;
            _entityStorage.OpenStorage(uid, storage);
            RemCompDeferred<ActiveSkrunglerComponent>(uid);
        }
    }

    private void OnInit(EntityUid uid, ActiveSkrunglerComponent component, ComponentInit args)
    {
        _appearance.SetData(uid, SkrunglerVisuals.Skrungling, true);
        _jitteringSystem.AddJitter(uid, -10, 100);
        _audio.PlayPvs(component.SkrungStartSound, uid);
        _audio.PlayPvs(component.SkrunglerSound, uid);
        _ambientSoundSystem.SetAmbience(uid, true);
    }

    private void OnShutdown(EntityUid uid, ActiveSkrunglerComponent component, ComponentShutdown args)
    {
        _appearance.SetData(uid, SkrunglerVisuals.Skrungling, false);
        RemComp<JitteringComponent>(uid);
        _audio.PlayPvs(component.SkrungFinishSound, uid);
        _ambientSoundSystem.SetAmbience(uid, false);
    }

    private void OnPowerChanged(EntityUid uid, SkrunglerComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
        {
            if (component.ProcessingTimer > 0)
                EnsureComp<ActiveSkrunglerComponent>(uid);
        }
        else
            RemComp<ActiveSkrunglerComponent>(uid);
    }

    private void OnUnanchorAttempt(EntityUid uid, ActiveSkrunglerComponent component, UnanchorAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRefreshParts(EntityUid uid, SkrunglerComponent component, RefreshPartsEvent args)
    {
        var laserRating = args.PartRatings[component.MachinePartProcessingSpeed];
        var manipRating = args.PartRatings[component.MachinePartYieldAmount];

        // Processing time slopes downwards with part rating.
        component.ProcessingTimePerUnitMass =
            component.BaseProcessingTimePerUnitMass / MathF.Pow(component.PartRatingSpeedMultiplier, laserRating - 1);

        // Yield slopes upwards with part rating.
        component.YieldPerUnitMass =
            component.BaseYieldPerUnitMass * MathF.Pow(component.PartRatingYieldAmountMultiplier, manipRating - 1);
    }

    private void OnUpgradeExamine(EntityUid uid, SkrunglerComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("skrungler-component-upgrade-speed", component.BaseProcessingTimePerUnitMass / component.ProcessingTimePerUnitMass);
        args.AddPercentageUpgrade("skrungler-component-upgrade-fuel-yield", component.YieldPerUnitMass / component.BaseYieldPerUnitMass);
    }
}
