using Content.Server.Body.Components;
using Content.Server.Construction;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Ghost;
using Content.Server.Stack;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._NF.Skrungler;
using Content.Shared._NF.Skrungler.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._NF.Skrungler;

/// <inheritdoc/>
public sealed class SkrunglerSystem : SharedSkrunglerSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkrunglerComponent, GetVerbsEvent<AlternativeVerb>>(AddSkrunglerVerb);
        SubscribeLocalEvent<SkrunglerComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<SkrunglerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SkrunglerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO: Move to shared when EntityStorage is in shared
        var query = EntityQueryEnumerator<SkrunglerComponent, EntityStorageComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var skrungler, out var storage, out var xform))
        {
            // Not active
            if (!skrungler.Active)
                continue;

            // Can't run if it requires power and isn't powered
            if (!_power.IsPowered(uid))
                continue;

            var curTime = Timing.CurTime;

            if (curTime > skrungler.NextMessTime)
            {
                if (_random.Prob(0.2f) && skrungler.BloodReagent is not null)
                {
                    Solution blood = new();
                    blood.AddReagent(skrungler.BloodReagent, 50);
                    _puddle.TrySpillAt(uid, blood, out _);
                }
                skrungler.NextMessTime = curTime + skrungler.MessInterval;
                // TODO perf: maybe use deltas for this? state is kinda big
                Dirty(uid, skrungler);
            }

            if (curTime < skrungler.FinishProcessingTime)
                continue;

            var actualYield = (int)skrungler.CurrentExpectedYield; // can only have integer
            skrungler.CurrentExpectedYield -= actualYield; // store non-integer leftovers

            var fuel = _stack.SpawnMultiple(skrungler.OutputStackType, actualYield, xform.Coordinates);
            foreach (var fuelEntity in fuel)
            {
                _containers.Insert(fuelEntity, storage.Contents);
            }

            skrungler.BloodReagent = null;
            _entityStorage.OpenStorage(uid, storage);
            EndProcessingVisuals((uid, skrungler));
            skrungler.Active = false;
            Dirty(uid, skrungler);
        }
    }

    private void AddSkrunglerVerb(Entity<SkrunglerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (ent.Comp.Active)
            return;

        // TODO: Prediction
        if (!TryComp(ent, out EntityStorageComponent? storage))
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || storage.Open)
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("skrungle-verb-activate"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => TryStartProcessing((ent, ent.Comp, storage)),
            Impact = LogImpact.High, // could be a body? or evidence? I dunno.
        };

        args.Verbs.Add(verb);
    }

    private void TryStartProcessing(Entity<SkrunglerComponent, EntityStorageComponent> ent)
    {
        if (ent.Comp2.Open || ent.Comp2.Contents.ContainedEntities.Count < 1)
            return;

        // Refuse to accept alive mobs and dead-but-connected players
        var containedEntity = ent.Comp2.Contents.ContainedEntities[0];
        if (containedEntity is not { Valid: true })
            return;

        if (TryComp<MobStateComponent>(containedEntity, out var comp) && !_mobState.IsDead(containedEntity, comp))
            return;

        if (_player.TryGetSessionByEntity(containedEntity, out var session) &&
            session.State.Status == SessionStatus.InGame)
        {
            return;
        }

        StartProcessing(containedEntity, ent);
    }

    protected override void StartProcessing(EntityUid uid, Entity<SkrunglerComponent> skrungler)
    {
        base.StartProcessing(uid, skrungler);

        if (TryComp<BloodstreamComponent>(uid, out var stream))
            skrungler.Comp.BloodReagent = stream.BloodReagent;
    }

    private void OnSuicideByEnvironment(Entity<SkrunglerComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Active)
            return;

        if (!_power.IsPowered(ent.Owner))
            return;

        if (_minds.TryGetMind(args.Victim, out var mindId, out var mind))
        {
            _ghost.OnGhostAttempt(mindId, false, mind: mind);

            if (mind.OwnedEntity is { Valid: true } entity)
                _popup.PopupEntity(Loc.GetString("skrungler-entity-storage-component-suicide-message"), entity);
        }

        _popup.PopupEntity(Loc.GetString("skrungler-entity-storage-component-suicide-message-others",
                ("victim", Identity.Entity(args.Victim, EntityManager))),
            args.Victim,
            Filter.PvsExcept(args.Victim),
            true,
            PopupType.LargeCaution);

        if (_entityStorage.CanInsert(args.Victim, ent))
        {
            _entityStorage.CloseStorage(ent);
            _standing.Down(args.Victim, false);
            _entityStorage.Insert(args.Victim, ent);
        }
        else
            Del(args.Victim);

        _entityStorage.CloseStorage(ent);
        StartProcessing(args.Victim, ent);
        args.Handled = true;
    }

    private void OnRefreshParts(Entity<SkrunglerComponent> ent, ref RefreshPartsEvent args)
    {
        var laserRating = args.PartRatings[ent.Comp.MachinePartProcessingSpeed];
        var manipRating = args.PartRatings[ent.Comp.MachinePartYieldAmount];

        // Processing time slopes downwards with part rating.
        ent.Comp.ProcessingTimePerUnitMass =
            ent.Comp.BaseProcessingTimePerUnitMass / MathF.Pow(ent.Comp.PartRatingSpeedMultiplier, laserRating - 1);

        // Yield slopes upwards with part rating.
        ent.Comp.YieldPerUnitMass =
            ent.Comp.BaseYieldPerUnitMass * MathF.Pow(ent.Comp.PartRatingYieldAmountMultiplier, manipRating - 1);

        Dirty(ent);
    }

    private void OnUpgradeExamine(Entity<SkrunglerComponent> ent, ref UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("skrungler-component-upgrade-speed",
            (float)(ent.Comp.BaseProcessingTimePerUnitMass / ent.Comp.ProcessingTimePerUnitMass));
        args.AddPercentageUpgrade("skrungler-component-upgrade-fuel-yield",
            ent.Comp.YieldPerUnitMass / ent.Comp.BaseYieldPerUnitMass);
    }
}
