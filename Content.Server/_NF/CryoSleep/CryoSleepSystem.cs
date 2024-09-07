using System.Numerics;
using Content.Server.DoAfter;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Interaction;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Shipyard.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Climbing.Systems;
using Content.Shared.CryoSleep;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared._NF.CCVar;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.CryoSleep;

public sealed partial class CryoSleepSystem : SharedCryoSleepSystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly ClimbSystem _climb = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ShipyardSystem _shipyard = default!; // For the FoundOrganics method

    private readonly Dictionary<NetUserId, StoredBody?> _storedBodies = new();
    private EntityUid? _storageMap;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoSleepComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<CryoSleepComponent, GetVerbsEvent<InteractionVerb>>(AddInsertOtherVerb);
        SubscribeLocalEvent<CryoSleepComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoSleepComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<CryoSleepComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CryoSleepComponent, DestructionEventArgs>((e,c,_) => EjectBody(e, c));
        SubscribeLocalEvent<CryoSleepComponent, CryoStoreDoAfterEvent>(OnAutoCryoSleep);
        SubscribeLocalEvent<CryoSleepComponent, DragDropTargetEvent>(OnEntityDragDropped);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);

        InitReturning();
    }

    private EntityUid GetStorageMap()
    {
        if (Deleted(_storageMap))
        {
            var map = _mapManager.CreateMap();
            _storageMap = _mapManager.GetMapEntityId(map);
            _mapManager.SetMapPaused(map, true);
        }

        return _storageMap.Value;
    }

    private void OnInit(EntityUid uid, CryoSleepComponent component, ComponentStartup args)
    {
        component.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, "body_container");
    }

    private void AddInsertOtherVerb(EntityUid uid, CryoSleepComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // If the user is currently holding/pulling an entity that can be cryo-sleeped, add a verb for that.
        if (args.Using is { Valid: true } @using &&
            !IsOccupied(component) &&
            _interaction.InRangeUnobstructed(@using, args.Target) &&
            _actionBlocker.CanMove(@using) &&
            HasComp<MindContainerComponent>(@using))
        {
            var name = "Unknown";
            if (TryComp<MetaDataComponent>(args.Using.Value, out var metadata))
                name = metadata.EntityName;

            InteractionVerb verb = new()
            {
                Act = () => InsertBody(@using, component, false),
                Category = VerbCategory.Insert,
                Text = name
            };
            args.Verbs.Add(verb);
        }
    }

    private void AddAlternativeVerbs(EntityUid uid, CryoSleepComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Eject verb
        if (IsOccupied(component))
        {
            AlternativeVerb verb = new()
            {
                Act = () => EjectBody(uid, component),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("medical-scanner-verb-noun-occupant")
            };
            args.Verbs.Add(verb);
        }

        // Self-insert verb
        if (!IsOccupied(component) &&
            (_actionBlocker.CanMove(args.User))) // || HasComp<WheelchairBoundComponent>(args.User))) // just get working legs
        {
            AlternativeVerb verb = new()
            {
                Act = () => InsertBody(args.User, component, false),
                Category = VerbCategory.Insert,
                Text = Loc.GetString("medical-scanner-verb-enter")
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnSuicide(EntityUid uid, CryoSleepComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;

        if (args.Victim != component.BodyContainer.ContainedEntity)
            return;

        QueueDel(args.Victim);
        _audio.PlayPvs(component.LeaveSound, uid);
        args.Handled = true;
    }

    private void OnExamine(EntityUid uid, CryoSleepComponent component, ExaminedEvent args)
    {
        var message = component.BodyContainer.ContainedEntity == null
            ? "cryopod-examine-empty"
            : "cryopod-examine-occupied";

        args.PushMarkup(Loc.GetString(message));
    }

    private void OnAutoCryoSleep(EntityUid uid, CryoSleepComponent component, CryoStoreDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var pod = args.Used;
        var body = args.Target;
        if (body is not { Valid: true } || pod is not { Valid: true })
            return;

        CryoStoreBody(body.Value, pod.Value);
        args.Handled = true;
    }

    private void OnEntityDragDropped(EntityUid uid, CryoSleepComponent component, DragDropTargetEvent args)
    {
        if (InsertBody(args.Dragged, component, false))
        {
            args.Handled = true;
        }
    }

    public bool InsertBody(EntityUid? toInsert, CryoSleepComponent component, bool force)
    {
        var cryopod = component.Owner;
        if (toInsert == null)
            return false;
        if (IsOccupied(component) && !force)
            return false;

        var mobQuery = GetEntityQuery<MobStateComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        // Refuse to accept "passengers" (e.g. pet felinids in bags)
        string? name = _shipyard.FoundOrganics(toInsert.Value, mobQuery, xformQuery);
        if (name is not null)
        {
            _popup.PopupEntity(Loc.GetString("cryopod-refuse-organic", ("cryopod", cryopod), ("name", name)), cryopod, PopupType.SmallCaution);
            return false;
        }

        // Refuse to accept dead or crit bodies, as well as non-mobs
        if (!TryComp<MobStateComponent>(toInsert, out var mob) || !_mobSystem.IsAlive(toInsert.Value, mob))
        {
            _popup.PopupEntity(Loc.GetString("cryopod-refuse-dead", ("cryopod", cryopod)), cryopod, PopupType.SmallCaution);
            return false;
        }

        // If the inserted player has disconnected, it will be stored immediately.
        if (_mind.TryGetMind(toInsert.Value, out var mind, out var mindComp))
        {
            var session = mindComp.Session;
            if (session is not null && session.Status == SessionStatus.Disconnected)
            {
                CryoStoreBody(toInsert.Value, cryopod);
                return true;
            }
        }

        var success = _container.Insert(toInsert.Value, component.BodyContainer);

        if (success && mindComp?.Session != null)
        {
            _euiManager.OpenEui(new CryoSleepEui(toInsert.Value,  cryopod, this), mindComp.Session);
        }

        if (success)
        {
            // Start a do-after event - if the inserted body is still inside and has not decided to sleep/leave, it will be stored.
            // It does not matter whether the entity has a mind or not.
            var ev = new CryoStoreDoAfterEvent();
            var args = new DoAfterArgs(
                _entityManager,
                toInsert.Value,
                TimeSpan.FromSeconds(30),
                ev,
                cryopod,
                toInsert,
                cryopod
            )
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = true
            };

            if (_doAfter.TryStartDoAfter(args))
                component.CryosleepDoAfter = ev.DoAfter.Id;
        }

        return success;
    }

    public void CryoStoreBody(EntityUid bodyId, EntityUid cryopod)
    {
        if (!TryComp<CryoSleepComponent>(cryopod, out var cryo))
            return;

        NetUserId? id = null;
        if (_mind.TryGetMind(bodyId, out var mindEntity, out var mind) && mind.CurrentEntity is { Valid : true } body)
        {
            var argMind = mind;
            RaiseLocalEvent(bodyId, new CryosleepBeforeMindRemovedEvent(cryopod, argMind?.UserId), true);
            _gameTicker.OnGhostAttempt(mindEntity, false, true, mind: mind);

            id = mind.UserId;
            if (id != null)
                _storedBodies[id.Value] = new StoredBody() { Body = body, Cryopod = cryopod };
        }

        var storage = GetStorageMap();
        var xform = Transform(bodyId);
        _container.Remove(bodyId, cryo.BodyContainer, reparent: false, force: true);
        xform.Coordinates = new EntityCoordinates(storage, Vector2.Zero);

        RaiseLocalEvent(bodyId, new CryosleepEnterEvent(cryopod, mind?.UserId), true);

        if (cryo.CryosleepDoAfter != null && _doAfter.GetStatus(cryo.CryosleepDoAfter) == DoAfterStatus.Running)
            _doAfter.Cancel(cryo.CryosleepDoAfter);

        // Start a timer. When it ends, the body needs to be deleted.
        Timer.Spawn(TimeSpan.FromSeconds(_configurationManager.GetCVar(NFCCVars.CryoExpirationTime)), () =>
        {
            if (id != null)
                ResetCryosleepState(id.Value);

            if (!Deleted(bodyId) && Transform(bodyId).ParentUid == _storageMap)
                QueueDel(bodyId);
        });
    }

    /// <param name="body">If not null, will not eject if the stored body is different from that parameter.</param>
    public bool EjectBody(EntityUid pod, CryoSleepComponent? component = null, EntityUid? body = null)
    {
        if (!Resolve(pod, ref component))
            return false;

        if (!IsOccupied(component) || (body != null && component.BodyContainer.ContainedEntity != body))
            return false;

        var toEject = component.BodyContainer.ContainedEntity;
        if (toEject == null)
            return false;

        _container.Remove(toEject.Value, component.BodyContainer, force: true);
        //_climb.ForciblySetClimbing(toEject.Value, pod);

        if (component.CryosleepDoAfter != null && _doAfter.GetStatus(component.CryosleepDoAfter) == DoAfterStatus.Running)
            _doAfter.Cancel(component.CryosleepDoAfter);

        return true;
    }

    private bool IsOccupied(CryoSleepComponent component)
    {
        return component.BodyContainer.ContainedEntity != null;
    }

    private void OnRoundEnded(RoundEndedEvent args)
    {
        _storedBodies.Clear();
    }

    private struct StoredBody
    {
        public EntityUid Body;
        public EntityUid Cryopod;
    }
}

